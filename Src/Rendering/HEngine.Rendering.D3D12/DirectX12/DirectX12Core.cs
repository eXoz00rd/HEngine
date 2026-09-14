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

#if DEBUG
        unsafe
        {
            ID3D12Debug* debugController = null;
            var debugGuid = ID3D12Debug.Guid;
            if (_d3d12.GetDebugInterface(ref debugGuid, (void**)&debugController) >= 0 && debugController != null)
            {
                debugController->EnableDebugLayer();
                debugController->Release();
            }
        }
#endif

        var result = _d3d12.CreateDevice<IUnknown, ID3D12Device>(default, D3DFeatureLevel.Level110, out _device);
        if (result < 0)
            throw new Exception($"Failed to create D3D12 device. HRESULT: {result:X8}");

#if DEBUG
        EnableDebugFeatures();
#endif

    }

#if DEBUG
    private unsafe void EnableDebugFeatures()
    {
        ID3D12InfoQueue* infoQueue = null;
        var deviceGuid = ID3D12InfoQueue.Guid;
        if (((IUnknown*)_device.Handle)->QueryInterface(&deviceGuid, (void**)&infoQueue) < 0 || infoQueue == null)
        {
            return;
        }

        using var queue = new ComPtr<ID3D12InfoQueue>(infoQueue);

        queue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
        queue.SetBreakOnSeverity(MessageSeverity.Error, true);
    }
#endif
}