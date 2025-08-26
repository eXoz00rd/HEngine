using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace HEngine.Rendering.DirectX12;

public class DirectX12SwapChain : IDisposable
{
    private const int FrameCount = 2;
    private readonly ComPtr<ID3D12Resource>[] _renderTargets = new ComPtr<ID3D12Resource>[FrameCount];
    private bool _disposed;
    private uint _rtvDescriptorSize;
    private ComPtr<ID3D12DescriptorHeap> _rtvHeap;

    private ComPtr<IDXGISwapChain3> _swapChain;
    private IWindow _window;

    public void Dispose()
    {
        if (_disposed)
            return;

        for (var i = 0; i < FrameCount; i++)
            _renderTargets[i].Dispose();

        _rtvHeap.Dispose();
        _swapChain.Dispose();
        _disposed = true;
    }

    public unsafe void Initialize(ComPtr<ID3D12Device> device, DirectX12CommandQueue commandQueue, IWindow window)
    {
        _window = window;

        using var dxgi = DXGI.GetApi();
        using var factory = dxgi.CreateDXGIFactory1<IDXGIFactory4>();

        var swapChainDesc = new SwapChainDesc1
        {
            BufferCount = FrameCount,
            Width = (uint)window.Size.X,
            Height = (uint)window.Size.Y,
            Format = Format.FormatR8G8B8A8Unorm,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new SampleDesc(1, 0)
        };

        IDXGISwapChain1* tempSwapChain;
        var hresult = factory.CreateSwapChainForHwnd((IUnknown*)commandQueue.Queue.Handle,
            window.Native!.Win32!.Value.Hwnd, in swapChainDesc, null, null, &tempSwapChain);

        if (hresult < 0)
            throw new Exception($"Failed to create swap chain. HRESULT: {hresult:X8}");

        _swapChain = new ComPtr<IDXGISwapChain3>((IDXGISwapChain3*)tempSwapChain);

        CreateDescriptorHeaps(device);
        CreateRenderTargets(device);
    }

    public uint GetCurrentBackBufferIndex()
    {
        return _swapChain.GetCurrentBackBufferIndex();
    }

    public void BeginFrame(ComPtr<ID3D12GraphicsCommandList> commandList, int frameIndex)
    {
        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier
            {
                PResource = _renderTargets[frameIndex],
                StateBefore = ResourceStates.Present,
                StateAfter = ResourceStates.RenderTarget,
                Subresource = D3D12.ResourceBarrierAllSubresources
            }
        };

        commandList.ResourceBarrier(1, ref barrier);

        var rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        rtvHandle.Ptr += (nuint)(frameIndex * _rtvDescriptorSize);

        unsafe
        {
            commandList.OMSetRenderTargets(1, &rtvHandle, false, (CpuDescriptorHandle*)null);
        }

        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = _window.Size.X,
            Height = _window.Size.Y,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        commandList.RSSetViewports(1, ref viewport);

        var scissorRect = new Box2D<int>(0, 0, _window.Size.X, _window.Size.Y);
        commandList.RSSetScissorRects(1, in scissorRect);
    }

    public void Clear(ComPtr<ID3D12GraphicsCommandList> commandList, Vector4 clearColor, int frameIndex)
    {
        var rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        rtvHandle.Ptr += (nuint)(frameIndex * _rtvDescriptorSize);

        unsafe
        {
            var color = stackalloc float[4];
            color[0] = clearColor.X;
            color[1] = clearColor.Y;
            color[2] = clearColor.Z;
            color[3] = clearColor.W;

            commandList.ClearRenderTargetView(rtvHandle, color, 0, (Box2D<int>*)null);
        }
    }

    public void EndFrame(ComPtr<ID3D12GraphicsCommandList> commandList, int frameIndex)
    {
        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier
            {
                PResource = _renderTargets[frameIndex],
                StateBefore = ResourceStates.RenderTarget,
                StateAfter = ResourceStates.Present,
                Subresource = D3D12.ResourceBarrierAllSubresources
            }
        };

        commandList.ResourceBarrier(1, ref barrier);
    }

    public void Present()
    {
        var result = _swapChain.Present(1, 0);
        if (result < 0)
            throw new Exception($"Failed to present. HRESULT: {result:X8}");
    }

    private void CreateDescriptorHeaps(ComPtr<ID3D12Device> device)
    {
        var rtvHeapDesc = new DescriptorHeapDesc
        {
            NumDescriptors = FrameCount,
            Type = DescriptorHeapType.Rtv,
            Flags = DescriptorHeapFlags.None
        };

        var result = device.CreateDescriptorHeap(in rtvHeapDesc, out _rtvHeap);
        if (result < 0)
            throw new Exception($"Failed to create RTV heap. HRESULT: {result:X8}");

        _rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
    }

    private void CreateRenderTargets(ComPtr<ID3D12Device> device)
    {
        var rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        for (uint i = 0; i < FrameCount; i++)
            unsafe
            {
                _renderTargets[i] = _swapChain.GetBuffer<ID3D12Resource>(i);
                device.CreateRenderTargetView(_renderTargets[i], null, rtvHandle);
                rtvHandle.Ptr += _rtvDescriptorSize;
            }
    }
}