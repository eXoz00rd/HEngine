using HEngine.Rendering.Contracts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using System.Numerics;

namespace HEngine.Rendering.DirectX12;

public class DirectX12Resources : IDisposable
{
    private ComPtr<ID3D12Resource> _vertexBuffer;
    private ComPtr<ID3D12Resource> _indexBuffer;
    private ComPtr<ID3D12Resource> _constantBuffer;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12Device> _device;
    private bool _disposed;

    public bool IsInitialized { get; private set; }

    public void Initialize(IRenderDevice device)
    {
        // TODO: Pobierz ComPtr<ID3D12Device> z IRenderDevice
        // Może być konieczne rzutowanie lub dodanie metody GetNativeDevice()
        
        InitializeRootSignature();
        InitializePipelineState();
        InitializeBuffers();
        
        IsInitialized = true;
    }

    private void InitializeRootSignature()
    {
        // TODO: Implementuj tworzenie root signature dla sprite rendering
        // Typowo zawiera constant buffer dla matrices i sampler dla textur
    }

    private void InitializePipelineState()
    {
        // TODO: Implementuj tworzenie pipeline state
        // Zawiera vertex shader, pixel shader, input layout, blend state itp.
    }

    private void InitializeBuffers()
    {
        // TODO: Implementuj tworzenie vertex buffer, index buffer i constant buffer
        // Vertex buffer dla quad (2 trójkąty), index buffer dla indices, constant buffer dla transforms
    }

    public void RenderQuad(Vector2 position, Vector2 size, Vector4 color)
    {
        if (!IsInitialized || _disposed) return;
        
        // TODO: Implementuj renderowanie quad z podanymi parametrami
        // 1. Zaktualizuj constant buffer z position, size, color
        // 2. Ustaw root signature i pipeline state
        // 3. Ustaw vertex i index buffers
        // 4. Wywołaj DrawIndexed
    }

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
}
