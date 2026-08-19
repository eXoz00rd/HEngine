using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.Devices;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Managers;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.PostProcessing;

/// <summary>
/// Production, GPU-backed <see cref="IPostProcessCommandContext"/>. Owns two ping-pong
/// HDR (R16G16B16A16_FLOAT) render targets and drives <see cref="DirectX12PostProcessPipelineManager"/>'s
/// PSO to run the ToneMapping fullscreen pass, actually touching the GPU instead of
/// only recording call counts like <see cref="NullPostProcessCommandContext"/>.
///
/// The first pass of the chain reads from <see cref="RenderTargetManager"/>'s HDR scene texture via
/// <see cref="PrepareSceneSource"/> instead of its own ping-pong slots, and the final pass resolves
/// into the swap-chain back buffer via <see cref="ResolveToBackBuffer"/> (tracks #45).
/// Only supports the ToneMapping pass; other <see cref="IPostProcessEffect"/>s are not backed by
/// a real shader here (see <see cref="SetConstantFloat"/>/<see cref="SetConstantInt"/>/<see cref="SetConstantFloat4"/>).
/// </summary>
public sealed class DirectX12PostProcessCommandContext : IPostProcessCommandContext, IDisposable
{
    private const Format RenderTargetFormat = Format.FormatR16G16B16A16Float;
    private const int RenderTargetCount = 2;

    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly DirectX12PostProcessPipelineManager _pipelineManager;
    private readonly IGraphicsDevice _graphicsDevice;
    private readonly RenderTargetManager _renderTargetManager;
    private readonly DescriptorHeapManager _descriptorHeapManager;
    private readonly ILogger<DirectX12PostProcessCommandContext>? _logger;
    private readonly PingPongRenderTargets _pingPong = new();

    private DirectX12CommandQueue _commandQueue = null!;
    private ComPtr<ID3D12Device> _device;
    private readonly ComPtr<ID3D12Resource>[] _renderTargets = new ComPtr<ID3D12Resource>[RenderTargetCount];
    private readonly ResourceStates[] _renderTargetStates = new ResourceStates[RenderTargetCount];
    private readonly DescriptorHandle[] _srvHandles = new DescriptorHandle[RenderTargetCount];
    private ComPtr<ID3D12DescriptorHeap> _rtvHeap;
    private uint _rtvDescriptorSize;

    private ToneMappingCbuffer _pendingConstants = ToneMappingCbuffer.Create(0, 1.0f, 2.2f);
    private bool _initialized;
    private bool _disposed;
    private bool _usePendingSceneSource;

    public int SourceRenderTargetIndex => _pingPong.CurrentSource;
    public int DestinationRenderTargetIndex => _pingPong.CurrentDestination;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int DrawCallCount { get; private set; }

    public DirectX12PostProcessCommandContext(
        DirectX12PostProcessPipelineManager pipelineManager,
        IGraphicsDevice graphicsDevice,
        RenderTargetManager renderTargetManager,
        DescriptorHeapManager descriptorHeapManager,
        ILogger<DirectX12PostProcessCommandContext>? logger = null)
    {
        _pipelineManager = pipelineManager ?? throw new ArgumentNullException(nameof(pipelineManager));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _renderTargetManager = renderTargetManager ?? throw new ArgumentNullException(nameof(renderTargetManager));
        _descriptorHeapManager = descriptorHeapManager ?? throw new ArgumentNullException(nameof(descriptorHeapManager));
        _logger = logger;

        Array.Fill(_srvHandles, DescriptorHandle.Invalid);
    }

    public void Initialize(ComPtr<ID3D12Device> device, int width, int height)
    {
        if (_initialized) Dispose();
        _disposed = false;

        _device = device;
        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);
        _commandQueue = ((DirectX12Device)_graphicsDevice).GetDirectX12CommandQueue();

        CreateRtvHeap();
        CreateRenderTargets();

        _pingPong.Reset();
        _initialized = true;
        _logger?.LogDebug("DirectX12PostProcessCommandContext initialized: {Width}x{Height}", Width, Height);
    }

    public void Resize(int width, int height)
    {
        if (!_initialized) return;

        _commandQueue.WaitForGpuIdle();

        for (var i = 0; i < RenderTargetCount; i++)
            _renderTargets[i].Dispose();

        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);

        CreateRenderTargets();
    }

    private void CreateRenderTargets()
    {
        for (var i = 0; i < RenderTargetCount; i++)
        {
            if (!_srvHandles[i].IsValid)
                _srvHandles[i] = _descriptorHeapManager.AllocateSrv();

            CreateRenderTarget(i);
            _renderTargetStates[i] = ResourceStates.RenderTarget;
        }
    }

    /// <summary>
    /// GPU resource backing one of the two logical ping-pong render targets
    /// (<see cref="PingPongRenderTargets.RenderTargetA"/> or <see cref="PingPongRenderTargets.RenderTargetB"/>)
    /// used to chain multiple post-process effects.
    /// </summary>
    public ComPtr<ID3D12Resource> GetRenderTarget(int index) => _renderTargets[index];

    public CpuDescriptorHandle GetRenderTargetRtvHandle(int index)
    {
        if (index < 0 || index >= RenderTargetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        var start = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        return new CpuDescriptorHandle { Ptr = start.Ptr + (nuint)(index * _rtvDescriptorSize) };
    }

    public void SetConstantFloat(string name, float value)
    {
        switch (name)
        {
            case "Exposure":
                _pendingConstants.Exposure = value;
                break;
            case "Gamma":
                _pendingConstants.Gamma = value;
                break;
            default:
                throw new NotSupportedException(
                    $"DirectX12PostProcessCommandContext only backs the ToneMapping pass; " +
                    $"'{name}' is not a supported float constant.");
        }
    }

    public void SetConstantInt(string name, int value)
    {
        if (name == "ToneMappingMode")
            _pendingConstants.ToneMappingMode = value;
        else
            throw new NotSupportedException(
                $"DirectX12PostProcessCommandContext only backs the ToneMapping pass; " +
                $"'{name}' is not a supported int constant.");
    }

    public void SetConstantFloat4(string name, float x, float y, float z, float w)
    {
        throw new NotSupportedException(
            $"DirectX12PostProcessCommandContext only backs the ToneMapping pass; " +
            $"'{name}' is not a supported float4 constant.");
    }

    /// <summary>
    /// Transitions <see cref="RenderTargetManager"/>'s HDR scene texture to a readable state and flags the
    /// next <see cref="DrawFullscreenTriangle"/> call to sample it directly, instead of this context's own
    /// ping-pong source slot, as the first pass's input (tracks #45).
    /// </summary>
    public void PrepareSceneSource()
    {
        if (!_initialized)
            throw new InvalidOperationException("DirectX12PostProcessCommandContext must be initialized before preparing the scene source.");

        _renderTargetManager.TransitionHdrTo(_commandQueue.CommandList, ResourceStates.PixelShaderResource);
        _usePendingSceneSource = true;
    }

    public unsafe void DrawFullscreenTriangle()
    {
        if (!_initialized)
            throw new InvalidOperationException("DirectX12PostProcessCommandContext must be initialized before drawing.");
        if (!_pipelineManager.IsInitialized)
            throw new InvalidOperationException("DirectX12PostProcessPipelineManager must be initialized before drawing.");

        var sourceIndex = SourceRenderTargetIndex;
        var destIndex = DestinationRenderTargetIndex;
        var useSceneSource = _usePendingSceneSource;
        _usePendingSceneSource = false;

        var commandList = _commandQueue.CommandList;

        if (!useSceneSource)
            TransitionTo(commandList, sourceIndex, ResourceStates.PixelShaderResource);
        TransitionTo(commandList, destIndex, ResourceStates.RenderTarget);

        var rtvHandle = GetRenderTargetRtvHandle(destIndex);
        CpuDescriptorHandle* rtvHandlePtr = &rtvHandle;
        commandList.OMSetRenderTargets(1, rtvHandlePtr, false, (CpuDescriptorHandle*)null);

        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = Width,
            Height = Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        commandList.RSSetViewports(1, ref viewport);

        var scissorRect = new Silk.NET.Maths.Box2D<int>(0, 0, Width, Height);
        commandList.RSSetScissorRects(1, in scissorRect);

        var heap = _descriptorHeapManager.SrvHeap;
        commandList.SetDescriptorHeaps(1, ref heap);

        commandList.SetPipelineState(_pipelineManager.PipelineState);
        commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
        commandList.SetGraphicsRoot32BitConstants(
            DirectX12PostProcessPipelineManager.ConstantsRootParameterIndex, 4, ref _pendingConstants, 0);

        var sourceSrvHandle = useSceneSource ? _renderTargetManager.GetHdrSrvGpuHandle() : _srvHandles[sourceIndex].GpuHandle;
        commandList.SetGraphicsRootDescriptorTable(
            DirectX12PostProcessPipelineManager.SourceTextureRootParameterIndex, sourceSrvHandle);

        commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        commandList.DrawInstanced(3, 1, 0, 0);

        TransitionTo(commandList, destIndex, ResourceStates.PixelShaderResource);

        DrawCallCount++;
        _pingPong.Flip();
    }

    /// <summary>
    /// Draws the post-process chain's final output (this context's current
    /// <see cref="SourceRenderTargetIndex"/>, left in PixelShaderResource state by the last
    /// <see cref="DrawFullscreenTriangle"/> call) into the swap-chain back buffer, using a tonemap-free
    /// passthrough PSO so the LDR result isn't re-processed (tracks #45).
    /// </summary>
    public unsafe void ResolveToBackBuffer()
    {
        if (!_initialized)
            throw new InvalidOperationException("DirectX12PostProcessCommandContext must be initialized before resolving.");
        if (!_pipelineManager.IsInitialized)
            throw new InvalidOperationException("DirectX12PostProcessPipelineManager must be initialized before resolving.");

        var sourceIndex = SourceRenderTargetIndex;
        var commandList = _commandQueue.CommandList;
        var backBufferRtv = ((DirectX12Device)_graphicsDevice).GetBackBufferRtvHandle();

        CpuDescriptorHandle* rtvHandlePtr = &backBufferRtv;
        commandList.OMSetRenderTargets(1, rtvHandlePtr, false, (CpuDescriptorHandle*)null);

        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = Width,
            Height = Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        commandList.RSSetViewports(1, ref viewport);

        var scissorRect = new Silk.NET.Maths.Box2D<int>(0, 0, Width, Height);
        commandList.RSSetScissorRects(1, in scissorRect);

        var heap = _descriptorHeapManager.SrvHeap;
        commandList.SetDescriptorHeaps(1, ref heap);

        commandList.SetPipelineState(_pipelineManager.BackBufferPipelineState);
        commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
        commandList.SetGraphicsRootDescriptorTable(
            DirectX12PostProcessPipelineManager.SourceTextureRootParameterIndex, _srvHandles[sourceIndex].GpuHandle);

        commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        commandList.DrawInstanced(3, 1, 0, 0);
    }

    private void TransitionTo(ComPtr<ID3D12GraphicsCommandList> commandList, int index, ResourceStates target)
    {
        if (_renderTargetStates[index] == target)
            return;

        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier
            {
                PResource = _renderTargets[index],
                StateBefore = _renderTargetStates[index],
                StateAfter = target,
                Subresource = D3D12.ResourceBarrierAllSubresources
            }
        };

        commandList.ResourceBarrier(1, ref barrier);
        _renderTargetStates[index] = target;
    }

    private unsafe void CreateRtvHeap()
    {
        var rtvDesc = new DescriptorHeapDesc
        {
            NumDescriptors = RenderTargetCount,
            Type = DescriptorHeapType.Rtv,
            Flags = DescriptorHeapFlags.None
        };
        var hr = _device.CreateDescriptorHeap(in rtvDesc, out _rtvHeap);
        if (hr < 0)
            throw new InvalidOperationException($"PostProcess RTV heap creation failed. HRESULT: {hr:X8}");

        _rtvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
    }

    private unsafe void CreateRenderTarget(int index)
    {
        var heapProps = new HeapProperties { Type = HeapType.Default };

        var resourceDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = (ulong)Width,
            Height = (uint)Height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = RenderTargetFormat,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.AllowRenderTarget
        };

        var clearValue = new ClearValue { Format = RenderTargetFormat };
        clearValue.Anonymous.Color[0] = 0.0f;
        clearValue.Anonymous.Color[1] = 0.0f;
        clearValue.Anonymous.Color[2] = 0.0f;
        clearValue.Anonymous.Color[3] = 1.0f;

        var result = _device.CreateCommittedResource(
            in heapProps,
            HeapFlags.None,
            in resourceDesc,
            ResourceStates.RenderTarget,
            &clearValue,
            out _renderTargets[index]);

        if (result < 0)
            throw new InvalidOperationException($"PostProcess render target {index} creation failed. HRESULT: {result:X8}");

        var rtvDesc = new RenderTargetViewDesc
        {
            Format = RenderTargetFormat,
            ViewDimension = RtvDimension.Texture2D
        };
        _device.CreateRenderTargetView(_renderTargets[index], &rtvDesc, GetRenderTargetRtvHandle(index));

        var srvDesc = new ShaderResourceViewDesc
        {
            Format = RenderTargetFormat,
            ViewDimension = SrvDimension.Texture2D,
            Shader4ComponentMapping = 0x00001688u
        };
        srvDesc.Anonymous.Texture2D.MostDetailedMip = 0;
        srvDesc.Anonymous.Texture2D.MipLevels = 1;

        _device.CreateShaderResourceView(_renderTargets[index], &srvDesc, _srvHandles[index].CpuHandle);
    }

    public void Dispose()
    {
        if (_disposed) return;

        for (var i = 0; i < RenderTargetCount; i++)
        {
            _renderTargets[i].Dispose();
            if (_srvHandles[i].IsValid)
            {
                _descriptorHeapManager.FreeSrv(_srvHandles[i]);
                _srvHandles[i] = DescriptorHandle.Invalid;
            }
        }

        _rtvHeap.Dispose();
        _d3d12.Dispose();

        _initialized = false;
        _disposed = true;
    }
}
