using HEngine.Rendering.Data;
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
/// </summary>
public sealed class DirectX12PostProcessCommandContext : IPostProcessCommandContext, IDisposable
{
    private const Format RenderTargetFormat = Format.FormatR16G16B16A16Float;

    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly DirectX12PostProcessPipelineManager _pipelineManager;
    private readonly DirectX12CommandQueue _commandQueue;
    private readonly ILogger<DirectX12PostProcessCommandContext>? _logger;
    private readonly PingPongRenderTargets _pingPong = new();

    private ComPtr<ID3D12Device> _device;
    private readonly ComPtr<ID3D12Resource>[] _renderTargets = new ComPtr<ID3D12Resource>[2];
    private readonly ResourceStates[] _renderTargetStates = new ResourceStates[2];
    private ComPtr<ID3D12DescriptorHeap> _rtvHeap;
    private ComPtr<ID3D12DescriptorHeap> _srvHeap;
    private uint _rtvDescriptorSize;
    private uint _srvDescriptorSize;

    private ToneMappingCbuffer _pendingConstants = ToneMappingCbuffer.Create(0, 1.0f, 2.2f);
    private bool _initialized;
    private bool _disposed;

    public int SourceRenderTargetIndex => _pingPong.CurrentSource;
    public int DestinationRenderTargetIndex => _pingPong.CurrentDestination;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int DrawCallCount { get; private set; }

    public DirectX12PostProcessCommandContext(
        DirectX12PostProcessPipelineManager pipelineManager,
        DirectX12CommandQueue commandQueue,
        ILogger<DirectX12PostProcessCommandContext>? logger = null)
    {
        _pipelineManager = pipelineManager ?? throw new ArgumentNullException(nameof(pipelineManager));
        _commandQueue = commandQueue ?? throw new ArgumentNullException(nameof(commandQueue));
        _logger = logger;
    }

    public void Initialize(ComPtr<ID3D12Device> device, int width, int height)
    {
        if (_initialized) Dispose();

        _device = device;
        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);

        CreateDescriptorHeaps();
        for (var i = 0; i < 2; i++)
        {
            CreateRenderTarget(i);
            _renderTargetStates[i] = ResourceStates.RenderTarget;
        }

        _pingPong.Reset();
        _initialized = true;
        _logger?.LogDebug("DirectX12PostProcessCommandContext initialized: {Width}x{Height}", Width, Height);
    }

    public void Resize(int width, int height)
    {
        if (!_initialized) return;

        for (var i = 0; i < 2; i++)
            _renderTargets[i].Dispose();

        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);

        for (var i = 0; i < 2; i++)
        {
            CreateRenderTarget(i);
            _renderTargetStates[i] = ResourceStates.RenderTarget;
        }
    }

    /// <summary>
    /// GPU resource backing one of the two logical render targets (<see cref="PingPongRenderTargets.RenderTargetA"/>
    /// or <see cref="PingPongRenderTargets.RenderTargetB"/>). Exposed so the main scene pass can be redirected
    /// to render into render target A before the post-process chain runs (tracked separately: #45).
    /// </summary>
    public ComPtr<ID3D12Resource> GetRenderTarget(int index) => _renderTargets[index];

    public CpuDescriptorHandle GetRenderTargetRtvHandle(int index)
    {
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
                _logger?.LogDebug("Ignoring unknown post-process float constant '{Name}'", name);
                break;
        }
    }

    public void SetConstantInt(string name, int value)
    {
        if (name == "ToneMappingMode")
            _pendingConstants.ToneMappingMode = value;
        else
            _logger?.LogDebug("Ignoring unknown post-process int constant '{Name}'", name);
    }

    public void SetConstantFloat4(string name, float x, float y, float z, float w)
    {
        _logger?.LogDebug("Ignoring unsupported post-process float4 constant '{Name}'", name);
    }

    public unsafe void DrawFullscreenTriangle()
    {
        if (!_initialized)
            throw new InvalidOperationException("DirectX12PostProcessCommandContext must be initialized before drawing.");

        var sourceIndex = SourceRenderTargetIndex;
        var destIndex = DestinationRenderTargetIndex;

        var commandList = _commandQueue.CommandList;

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

        var heap = _srvHeap;
        commandList.SetDescriptorHeaps(1, ref heap);

        commandList.SetPipelineState(_pipelineManager.PipelineState);
        commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
        commandList.SetGraphicsRoot32BitConstants(
            DirectX12PostProcessPipelineManager.ConstantsRootParameterIndex, 4, ref _pendingConstants, 0);
        commandList.SetGraphicsRootDescriptorTable(
            DirectX12PostProcessPipelineManager.SourceTextureRootParameterIndex, GetSrvGpuHandle(sourceIndex));

        commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        commandList.DrawInstanced(3, 1, 0, 0);

        TransitionTo(commandList, destIndex, ResourceStates.PixelShaderResource);

        DrawCallCount++;
        _pingPong.Flip();
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

    private GpuDescriptorHandle GetSrvGpuHandle(int index)
    {
        var start = _srvHeap.GetGPUDescriptorHandleForHeapStart();
        return new GpuDescriptorHandle { Ptr = start.Ptr + (ulong)(index * _srvDescriptorSize) };
    }

    private unsafe void CreateDescriptorHeaps()
    {
        var rtvDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 2,
            Type = DescriptorHeapType.Rtv,
            Flags = DescriptorHeapFlags.None
        };
        var hr = _device.CreateDescriptorHeap(in rtvDesc, out _rtvHeap);
        if (hr < 0)
            throw new InvalidOperationException($"PostProcess RTV heap creation failed. HRESULT: {hr:X8}");

        var srvDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 2,
            Type = DescriptorHeapType.CbvSrvUav,
            Flags = DescriptorHeapFlags.ShaderVisible
        };
        hr = _device.CreateDescriptorHeap(in srvDesc, out _srvHeap);
        if (hr < 0)
            throw new InvalidOperationException($"PostProcess SRV heap creation failed. HRESULT: {hr:X8}");

        _rtvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        _srvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
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

        var srvCpuHandle = _srvHeap.GetCPUDescriptorHandleForHeapStart();
        srvCpuHandle.Ptr += (nuint)(index * _srvDescriptorSize);
        _device.CreateShaderResourceView(_renderTargets[index], &srvDesc, srvCpuHandle);
    }

    public void Dispose()
    {
        if (_disposed) return;

        for (var i = 0; i < 2; i++)
            _renderTargets[i].Dispose();

        _rtvHeap.Dispose();
        _srvHeap.Dispose();
        _d3d12.Dispose();

        _initialized = false;
        _disposed = true;
    }
}
