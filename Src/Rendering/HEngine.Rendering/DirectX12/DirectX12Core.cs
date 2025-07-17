using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.DirectX12;

public class DirectX12Core : IDisposable
{
    private D3D12 _d3d12 = null!;
    private ComPtr<ID3D12Device> _device;
    private bool _disposed;

    public ComPtr<ID3D12Device> Device => _device;

    public void Dispose()
    {
        if (_disposed)
            return;

        _device.Dispose();
        _d3d12.Dispose();
        _disposed = true;
    }

    public void Initialize()
    {
        _d3d12 = D3D12.GetApi();

        var result = _d3d12.CreateDevice<IUnknown, ID3D12Device>(default, D3DFeatureLevel.Level110, out _device);
        if (result < 0)
            throw new Exception($"Failed to create D3D12 device. HRESULT: {result:X8}");

        Console.WriteLine("DirectX12Core initialized successfully");
    }
}