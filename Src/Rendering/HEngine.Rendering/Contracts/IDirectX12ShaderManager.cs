namespace HEngine.Core.Rendering.DirectX12.Contracts;

public interface IDirectX12ShaderManager : IDisposable
{
    ComPtr<ID3D10Blob> VertexShader { get; }
    ComPtr<ID3D10Blob> PixelShader { get; }
    void Initialize();
}