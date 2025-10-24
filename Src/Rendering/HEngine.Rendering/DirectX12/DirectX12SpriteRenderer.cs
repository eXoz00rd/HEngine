using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.DirectX12;

public class DirectX12Resources : IDisposable
{
    private ComPtr<ID3D12Resource> _constantBuffer;
    private ComPtr<ID3D12Device> _device;
    private bool _disposed;
    private ComPtr<ID3D12Resource> _indexBuffer;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12Resource> _vertexBuffer;

    public bool IsInitialized { get; private set; }

    public void Dispose()
    {
        if (_disposed) return;

        _pipelineState.Dispose();
        _rootSignature.Dispose();
        _constantBuffer.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _device.Dispose();

        _disposed = true;
    }

    public void Initialize(IRenderDevice device)
    {
        InitializeRootSignature();
        InitializePipelineState();
        InitializeBuffers();

        IsInitialized = true;
    }

    private void InitializeRootSignature()
    {
       
    }

    private void InitializePipelineState()
    {
     
    }

    private void InitializeBuffers()
    {
      
    }

    public void RenderQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        if (!IsInitialized || _disposed) return;
    }
}