using HEngine.Rendering.Data;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

/// <summary>
/// Manages GPU resources for cascaded shadow maps.
/// Creates a Texture2DArray depth resource (one slice per cascade),
/// DSVs for rendering into each slice, and a single SRV for sampling in the main pass.
/// </summary>
public sealed class ShadowMapManager : IDisposable
{
    private readonly D3D12 _d3d12 = D3D12.GetApi();
    private readonly ILogger<ShadowMapManager>? _logger;

    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12Resource> _shadowTexture;
    private ComPtr<ID3D12DescriptorHeap> _dsvHeap;
    private ComPtr<ID3D12DescriptorHeap> _srvHeap;

    private uint _dsvDescriptorSize;
    private int _resolution;
    private int _cascadeCount;
    private bool _initialized;
    private bool _disposed;

    public bool IsInitialized => _initialized;
    public int Resolution => _resolution;
    public int CascadeCount => _cascadeCount;

    public ShadowCbuffer ShadowConstants { get; private set; }

    public bool HasShadowData { get; private set; }

    public ShadowMapManager(ILogger<ShadowMapManager>? logger = null)
    {
        _logger = logger;
    }

    public void SetShadowConstants(ShadowCbuffer constants)
    {
        ShadowConstants = constants;
        HasShadowData = true;
    }

    public void Initialize(ComPtr<ID3D12Device> device, int resolution, int cascadeCount)
    {
        if (_initialized) ReleaseGpuResources();

        _device = device;
        _resolution = Math.Max(resolution, 64);
        _cascadeCount = Math.Clamp(cascadeCount, 1, 4);
        ShadowConstants = default;
        HasShadowData = false;

        CreateDescriptorHeaps();
        CreateShadowTextureArray();
        CreateDsvs();
        CreateSrv();

        _initialized = true;
        _logger?.LogDebug(
            "ShadowMapManager initialized: {Width}x{Height} × {Cascades} cascades",
            _resolution, _resolution, _cascadeCount);
    }

    public CpuDescriptorHandle GetDsvHandle(int cascade)
    {
        var start = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
        return new CpuDescriptorHandle
        {
            Ptr = start.Ptr + (nuint)(cascade * _dsvDescriptorSize)
        };
    }

    public CpuDescriptorHandle GetSrvCpuHandle()
    {
        return _srvHeap.GetCPUDescriptorHandleForHeapStart();
    }

    public GpuDescriptorHandle GetSrvGpuHandle()
    {
        return _srvHeap.GetGPUDescriptorHandleForHeapStart();
    }

    public unsafe void WriteSrvTo(CpuDescriptorHandle destination)
    {
        var srvDesc = new ShaderResourceViewDesc
        {
            Format = Format.FormatR32Float,
            ViewDimension = SrvDimension.Texture2Darray,
            Shader4ComponentMapping = 0x00001688u
        };
        srvDesc.Anonymous.Texture2DArray.MostDetailedMip = 0;
        srvDesc.Anonymous.Texture2DArray.MipLevels = 1;
        srvDesc.Anonymous.Texture2DArray.FirstArraySlice = 0;
        srvDesc.Anonymous.Texture2DArray.ArraySize = (uint)_cascadeCount;

        _device.CreateShaderResourceView(_shadowTexture, &srvDesc, destination);
    }

    public ComPtr<ID3D12Resource> ShadowTexture => _shadowTexture;

    public ComPtr<ID3D12DescriptorHeap> SrvHeap => _srvHeap;

    public void Resize(int resolution, int cascadeCount)
    {
        if (!_initialized) return;

        Initialize(_device, resolution, cascadeCount);
    }

    private unsafe void CreateDescriptorHeaps()
    {
        var dsvDesc = new DescriptorHeapDesc
        {
            NumDescriptors = (uint)_cascadeCount,
            Type = DescriptorHeapType.Dsv,
            Flags = DescriptorHeapFlags.None
        };
        var hr = _device.CreateDescriptorHeap(in dsvDesc, out _dsvHeap);
        if (hr < 0)
            throw new InvalidOperationException($"Shadow DSV heap creation failed. HRESULT: {hr:X8}");

        var srvDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 1,
            Type = DescriptorHeapType.CbvSrvUav,
            Flags = DescriptorHeapFlags.ShaderVisible
        };
        hr = _device.CreateDescriptorHeap(in srvDesc, out _srvHeap);
        if (hr < 0)
            throw new InvalidOperationException($"Shadow SRV heap creation failed. HRESULT: {hr:X8}");

        _dsvDescriptorSize = _device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Dsv);
    }

    private unsafe void CreateShadowTextureArray()
    {
        var heapProps = new HeapProperties { Type = HeapType.Default };

        var resDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = (ulong)_resolution,
            Height = (uint)_resolution,
            DepthOrArraySize = (ushort)_cascadeCount,
            MipLevels = 1,
            Format = Format.FormatR32Typeless,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.AllowDepthStencil
        };

        var clearValue = new ClearValue
        {
            Format = Format.FormatD32Float
        };
        clearValue.Anonymous.DepthStencil = new DepthStencilValue { Depth = 1.0f, Stencil = 0 };

        var hr = _device.CreateCommittedResource(
            in heapProps,
            HeapFlags.None,
            in resDesc,
            ResourceStates.DepthWrite,
            &clearValue,
            out _shadowTexture);

        if (hr < 0)
            throw new InvalidOperationException($"Shadow texture array creation failed. HRESULT: {hr:X8}");
    }

    private unsafe void CreateDsvs()
    {
        for (int i = 0; i < _cascadeCount; i++)
        {
            var dsvDesc = new DepthStencilViewDesc
            {
                Format = Format.FormatD32Float,
                ViewDimension = DsvDimension.Texture2Darray
            };
            dsvDesc.Anonymous.Texture2DArray.MipSlice = 0;
            dsvDesc.Anonymous.Texture2DArray.FirstArraySlice = (uint)i;
            dsvDesc.Anonymous.Texture2DArray.ArraySize = 1;

            _device.CreateDepthStencilView(_shadowTexture, &dsvDesc, GetDsvHandle(i));
        }
    }

    private unsafe void CreateSrv()
    {
        var srvDesc = new ShaderResourceViewDesc
        {
            Format = Format.FormatR32Float,
            ViewDimension = SrvDimension.Texture2Darray,
            Shader4ComponentMapping = 0x00001688u
        };
        srvDesc.Anonymous.Texture2DArray.MostDetailedMip = 0;
        srvDesc.Anonymous.Texture2DArray.MipLevels = 1;
        srvDesc.Anonymous.Texture2DArray.FirstArraySlice = 0;
        srvDesc.Anonymous.Texture2DArray.ArraySize = (uint)_cascadeCount;

        _device.CreateShaderResourceView(
            _shadowTexture,
            &srvDesc,
            GetSrvCpuHandle());
    }

    private void ReleaseGpuResources()
    {
        _shadowTexture.Dispose();
        _dsvHeap.Dispose();
        _srvHeap.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;

        ReleaseGpuResources();
        _d3d12.Dispose();

        ShadowConstants = default;
        HasShadowData = false;
        _initialized = false;
        _disposed = true;
    }
}


