using System.Numerics;
using HEngine.Rendering.Devices;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

/// <summary>
/// Manages HDR render targets and depth buffers for the PBR pipeline.
/// HDR target: DXGI_FORMAT_R16G16B16A16_FLOAT
/// Depth buffer: DXGI_FORMAT_D32_FLOAT
/// </summary>
public sealed class RenderTargetManager : IDisposable
{
    private const Format HdrFormat = Format.FormatR16G16B16A16Float;

    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly DescriptorHeapManager _descriptorHeapManager;
    private readonly ILogger<RenderTargetManager>? _logger;

    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12DescriptorHeap> _rtvHeap;
    private ComPtr<ID3D12DescriptorHeap> _dsvHeap;
    private ComPtr<ID3D12Resource> _hdrRenderTarget;
    private ComPtr<ID3D12Resource> _depthBuffer;
    private DescriptorHandle _hdrSrvHandle = DescriptorHandle.Invalid;
    private ResourceStates _hdrState = ResourceStates.RenderTarget;

    private uint _rtvDescriptorSize;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _initialized;

    public bool IsInitialized => _initialized;
    public int Width => _width;
    public int Height => _height;

    public RenderTargetManager(DescriptorHeapManager descriptorHeapManager, ILogger<RenderTargetManager>? logger = null)
    {
        _descriptorHeapManager = descriptorHeapManager ?? throw new ArgumentNullException(nameof(descriptorHeapManager));
        _logger = logger;
    }

    public void Initialize(ComPtr<ID3D12Device> device, int width, int height)
    {
        unsafe
        {
            if (device.Handle == null)
            {
                throw new ArgumentException(
                    "RenderTargetManager.Initialize was called with a null ID3D12Device handle; GPU resource " +
                    "creation would dereference a null device pointer.", nameof(device));
            }
        }

        if (_initialized)
            Dispose();
        _disposed = false;

        _device = device;
        _width = width;
        _height = height;
        _hdrState = ResourceStates.RenderTarget;

        CreateDescriptorHeaps();
        CreateHdrRenderTarget();
        CreateDepthBuffer();

        _initialized = true;
        _logger?.LogDebug("RenderTargetManager initialized: {Width}x{Height}", width, height);
    }

    public void Resize(int width, int height)
    {
        if (!_initialized)
            return;

        _hdrRenderTarget.Dispose();
        _depthBuffer.Dispose();

        _width = width;
        _height = height;
        _hdrState = ResourceStates.RenderTarget;

        CreateHdrRenderTarget();
        CreateDepthBuffer();

        _logger?.LogDebug("RenderTargetManager resized: {Width}x{Height}", width, height);
    }

    public CpuDescriptorHandle GetHdrRtvHandle()
    {
        return _rtvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    public CpuDescriptorHandle GetDsvHandle()
    {
        return _dsvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    public GpuDescriptorHandle GetHdrSrvGpuHandle()
    {
        return _hdrSrvHandle.GpuHandle;
    }

    public ComPtr<ID3D12Resource> HdrRenderTarget => _hdrRenderTarget;
    public ComPtr<ID3D12Resource> DepthBuffer => _depthBuffer;

    /// <summary>
    /// Binds the HDR color target + depth buffer as the current render target, transitioning
    /// the HDR resource to <see cref="ResourceStates.RenderTarget"/> first if needed.
    /// </summary>
    public unsafe void Bind(ComPtr<ID3D12GraphicsCommandList> commandList)
    {
        TransitionHdrTo(commandList, ResourceStates.RenderTarget);

        var rtvHandle = GetHdrRtvHandle();
        var dsvHandle = GetDsvHandle();
        commandList.OMSetRenderTargets(1, &rtvHandle, false, &dsvHandle);

        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = _width,
            Height = _height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        commandList.RSSetViewports(1, ref viewport);

        var scissorRect = new Silk.NET.Maths.Box2D<int>(0, 0, _width, _height);
        commandList.RSSetScissorRects(1, in scissorRect);
    }

    public unsafe void Clear(ComPtr<ID3D12GraphicsCommandList> commandList, Vector4 clearColor)
    {
        var rtvHandle = GetHdrRtvHandle();
        var dsvHandle = GetDsvHandle();

        var color = stackalloc float[4];
        color[0] = clearColor.X;
        color[1] = clearColor.Y;
        color[2] = clearColor.Z;
        color[3] = clearColor.W;

        commandList.ClearRenderTargetView(rtvHandle, color, 0, (Silk.NET.Maths.Box2D<int>*)null);
        commandList.ClearDepthStencilView(dsvHandle, ClearFlags.Depth, 1.0f, 0, 0, (Silk.NET.Maths.Box2D<int>*)null);
    }

    /// <summary>
    /// Transitions the HDR color target between being written to (RenderTarget, during the scene pass)
    /// and being read from (PixelShaderResource, as the post-process chain's first source). No-op if
    /// already in the requested state.
    /// </summary>
    public void TransitionHdrTo(ComPtr<ID3D12GraphicsCommandList> commandList, ResourceStates target)
    {
        if (_hdrState == target)
            return;

        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier
            {
                PResource = _hdrRenderTarget,
                StateBefore = _hdrState,
                StateAfter = target,
                Subresource = D3D12.ResourceBarrierAllSubresources
            }
        };

        commandList.ResourceBarrier(1, ref barrier);
        _hdrState = target;
    }

    private unsafe void CreateDescriptorHeaps()
    {
        var rtvHeapDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 1,
            Type = DescriptorHeapType.Rtv,
            Flags = DescriptorHeapFlags.None
        };
        var result = _device.CreateDescriptorHeap(in rtvHeapDesc, out _rtvHeap);
        if (result < 0)
            throw new InvalidOperationException($"Failed to create RTV descriptor heap. HRESULT: {result:X8}");

        var dsvHeapDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 1,
            Type = DescriptorHeapType.Dsv,
            Flags = DescriptorHeapFlags.None
        };
        result = _device.CreateDescriptorHeap(in dsvHeapDesc, out _dsvHeap);
        if (result < 0)
            throw new InvalidOperationException($"Failed to create DSV descriptor heap. HRESULT: {result:X8}");

        _rtvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
    }

    private unsafe void CreateHdrRenderTarget()
    {
        var heapProps = new HeapProperties { Type = HeapType.Default };

        var resourceDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = (ulong)_width,
            Height = (uint)_height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.FormatR16G16B16A16Float,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.AllowRenderTarget
        };

        var clearValue = new ClearValue
        {
            Format = Format.FormatR16G16B16A16Float
        };
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
            out _hdrRenderTarget);

        if (result < 0)
            throw new InvalidOperationException($"Failed to create HDR render target. HRESULT: {result:X8}");

        var rtvDesc = new RenderTargetViewDesc
        {
            Format = Format.FormatR16G16B16A16Float,
            ViewDimension = RtvDimension.Texture2D
        };

        _device.CreateRenderTargetView(_hdrRenderTarget, &rtvDesc, GetHdrRtvHandle());

        if (!_hdrSrvHandle.IsValid)
            _hdrSrvHandle = _descriptorHeapManager.AllocateSrv();

        var srvDesc = new ShaderResourceViewDesc
        {
            Format = HdrFormat,
            ViewDimension = SrvDimension.Texture2D,
            Shader4ComponentMapping = 0x00001688u
        };
        srvDesc.Anonymous.Texture2D.MostDetailedMip = 0;
        srvDesc.Anonymous.Texture2D.MipLevels = 1;

        _device.CreateShaderResourceView(_hdrRenderTarget, &srvDesc, _hdrSrvHandle.CpuHandle);
    }

    private unsafe void CreateDepthBuffer()
    {
        var heapProps = new HeapProperties { Type = HeapType.Default };

        var resourceDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = (ulong)_width,
            Height = (uint)_height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.FormatD32Float,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.AllowDepthStencil
        };

        var clearValue = new ClearValue
        {
            Format = Format.FormatD32Float
        };
        clearValue.Anonymous.DepthStencil = new DepthStencilValue { Depth = 1.0f, Stencil = 0 };

        var result = _device.CreateCommittedResource(
            in heapProps,
            HeapFlags.None,
            in resourceDesc,
            ResourceStates.DepthWrite,
            &clearValue,
            out _depthBuffer);

        if (result < 0)
            throw new InvalidOperationException($"Failed to create depth buffer. HRESULT: {result:X8}");

        var dsvDesc = new DepthStencilViewDesc
        {
            Format = Format.FormatD32Float,
            ViewDimension = DsvDimension.Texture2D
        };

        _device.CreateDepthStencilView(_depthBuffer, &dsvDesc, GetDsvHandle());
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _depthBuffer.Dispose();
        _hdrRenderTarget.Dispose();
        _dsvHeap.Dispose();
        _rtvHeap.Dispose();
        _d3d12.Dispose();

        if (_hdrSrvHandle.IsValid)
        {
            _descriptorHeapManager.FreeSrv(_hdrSrvHandle);
            _hdrSrvHandle = DescriptorHandle.Invalid;
        }

        _initialized = false;
        _disposed = true;
    }
}

