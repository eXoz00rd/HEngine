using System.Numerics;
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
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly ILogger<RenderTargetManager>? _logger;

    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12DescriptorHeap> _rtvHeap;
    private ComPtr<ID3D12DescriptorHeap> _dsvHeap;
    private ComPtr<ID3D12Resource> _hdrRenderTarget;
    private ComPtr<ID3D12Resource> _depthBuffer;

    private uint _rtvDescriptorSize;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _initialized;

    public bool IsInitialized => _initialized;
    public int Width => _width;
    public int Height => _height;

    public RenderTargetManager(ILogger<RenderTargetManager>? logger = null)
    {
        _logger = logger;
    }

    public void Initialize(ComPtr<ID3D12Device> device, int width, int height)
    {
        if (_initialized)
            Dispose();

        _device = device;
        _width = width;
        _height = height;

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

    public ComPtr<ID3D12Resource> HdrRenderTarget => _hdrRenderTarget;
    public ComPtr<ID3D12Resource> DepthBuffer => _depthBuffer;

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

        _initialized = false;
        _disposed = true;
    }
}

