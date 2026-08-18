using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace HEngine.Rendering.DirectX12;

public class DirectX12SwapChain : IDisposable
{
    private const int FrameCount = 3;
    private const Format DepthFormat = Format.FormatD32Float;
    private readonly ComPtr<ID3D12Resource>[] _renderTargets = new ComPtr<ID3D12Resource>[FrameCount];
    private bool _disposed;
    private uint _rtvDescriptorSize;
    private ComPtr<ID3D12DescriptorHeap> _rtvHeap;
    private ComPtr<ID3D12DescriptorHeap> _dsvHeap;
    private ComPtr<ID3D12Resource> _depthBuffer;
    private int _bufferWidth;
    private int _bufferHeight;

    private ComPtr<IDXGISwapChain3> _swapChain;

    public bool VSyncEnabled { get; set; } = true;
    public bool TearingSupported { get; private set; }
    public int TargetFrameRate { get; set; } = 0;

    public void Dispose()
    {
        if (_disposed)
            return;

        for (var i = 0; i < FrameCount; i++)
            _renderTargets[i].Dispose();

        _depthBuffer.Dispose();
        _dsvHeap.Dispose();
        _rtvHeap.Dispose();
        _swapChain.Dispose();
        _disposed = true;
    }

    public unsafe void Initialize(ComPtr<ID3D12Device> device, DirectX12CommandQueue commandQueue, IWindow window)
    {
        _bufferWidth = window.Size.X;
        _bufferHeight = window.Size.Y;

        using var dxgi = DXGI.GetApi(window);
        using var factory = dxgi.CreateDXGIFactory1<IDXGIFactory4>();

        TearingSupported = false;
        IDXGIFactory5* factory5Ptr = null;
        var factoryGuid = IDXGIFactory5.Guid;
        var qiResult = ((IUnknown*)factory.Handle)->QueryInterface(&factoryGuid, (void**)&factory5Ptr);
        if (qiResult >= 0 && factory5Ptr != null)
        {
            using var factory5 = new ComPtr<IDXGIFactory5>(factory5Ptr);
            int allowTearing = 0;
            var result = factory5.CheckFeatureSupport(Silk.NET.DXGI.Feature.PresentAllowTearing, &allowTearing, (uint)sizeof(int));
            TearingSupported = result >= 0 && allowTearing != 0;
        }

        var swapChainDesc = new SwapChainDesc1
        {
            BufferCount = FrameCount,
            Width = (uint)_bufferWidth,
            Height = (uint)_bufferHeight,
            Format = Format.FormatR8G8B8A8Unorm,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new SampleDesc(1, 0),
            Flags = TearingSupported ? (uint)SwapChainFlag.AllowTearing : 0u
        };

        IDXGISwapChain1* tempSwapChain;
        var hresult = factory.CreateSwapChainForHwnd((IUnknown*)commandQueue.Queue.Handle,
            window.Native!.Win32!.Value.Hwnd, in swapChainDesc, null, null, &tempSwapChain);

        if (hresult < 0)
            throw new Exception($"Failed to create swap chain. HRESULT: {hresult:X8}");

        _swapChain = new ComPtr<IDXGISwapChain3>((IDXGISwapChain3*)tempSwapChain);

        CreateDescriptorHeaps(device);
        CreateRenderTargets(device);
        CreateDepthBuffer(device);
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

        BindMainRenderTarget(commandList, frameIndex);
    }

    public void BindMainRenderTarget(ComPtr<ID3D12GraphicsCommandList> commandList, int frameIndex)
    {
        var rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        rtvHandle.Ptr += (nuint)(frameIndex * _rtvDescriptorSize);
        var dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();

        unsafe
        {
            commandList.OMSetRenderTargets(1, &rtvHandle, false, &dsvHandle);
        }

        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = _bufferWidth,
            Height = _bufferHeight,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        commandList.RSSetViewports(1, ref viewport);

        var scissorRect = new Box2D<int>(0, 0, _bufferWidth, _bufferHeight);
        commandList.RSSetScissorRects(1, in scissorRect);
    }

    public void Clear(ComPtr<ID3D12GraphicsCommandList> commandList, Vector4 clearColor, int frameIndex)
    {
        var rtvHandle = _rtvHeap.GetCPUDescriptorHandleForHeapStart();
        rtvHandle.Ptr += (nuint)(frameIndex * _rtvDescriptorSize);
        var dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();

        unsafe
        {
            var color = stackalloc float[4];
            color[0] = clearColor.X;
            color[1] = clearColor.Y;
            color[2] = clearColor.Z;
            color[3] = clearColor.W;

            commandList.ClearRenderTargetView(rtvHandle, color, 0, (Box2D<int>*)null);
            commandList.ClearDepthStencilView(dsvHandle, ClearFlags.Depth, 1.0f, 0, 0, (Box2D<int>*)null);
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
        uint syncInterval = VSyncEnabled ? 1u : 0u;
        uint presentFlags = 0u;

        if (!VSyncEnabled && TearingSupported)
        {
            presentFlags = DXGI.PresentAllowTearing;
        }

        var result = _swapChain.Present(syncInterval, presentFlags);
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

        var dsvHeapDesc = new DescriptorHeapDesc
        {
            NumDescriptors = 1,
            Type = DescriptorHeapType.Dsv,
            Flags = DescriptorHeapFlags.None
        };

        result = device.CreateDescriptorHeap(in dsvHeapDesc, out _dsvHeap);
        if (result < 0)
            throw new Exception($"Failed to create DSV heap. HRESULT: {result:X8}");
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

    private unsafe void CreateDepthBuffer(ComPtr<ID3D12Device> device)
    {
        var heapProps = new HeapProperties { Type = HeapType.Default };

        var resourceDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = (ulong)_bufferWidth,
            Height = (uint)_bufferHeight,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DepthFormat,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.AllowDepthStencil
        };

        var clearValue = new ClearValue
        {
            Format = DepthFormat
        };
        clearValue.Anonymous.DepthStencil = new DepthStencilValue { Depth = 1.0f, Stencil = 0 };

        var result = device.CreateCommittedResource(
            in heapProps,
            HeapFlags.None,
            in resourceDesc,
            ResourceStates.DepthWrite,
            &clearValue,
            out _depthBuffer);

        if (result < 0)
            throw new Exception($"Failed to create depth buffer. HRESULT: {result:X8}");

        var dsvDesc = new DepthStencilViewDesc
        {
            Format = DepthFormat,
            ViewDimension = DsvDimension.Texture2D
        };

        var dsvHandle = _dsvHeap.GetCPUDescriptorHandleForHeapStart();
        device.CreateDepthStencilView(_depthBuffer, &dsvDesc, dsvHandle);
    }
}