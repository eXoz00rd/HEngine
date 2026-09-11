using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Managers;

namespace HEngine.Rendering.Adapters;

public class DirectX12ShaderManagerAdapter : IShaderManager
{
    private readonly DirectX12ShaderManager _shaderManager;
    private bool _disposed;

    public DirectX12ShaderManagerAdapter(DirectX12ShaderManager shaderManager)
    {
        _shaderManager = shaderManager ?? throw new ArgumentNullException(nameof(shaderManager));
    }

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        _shaderManager.Initialize();
        IsInitialized = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _shaderManager?.Dispose();
        _disposed = true;
    }
}