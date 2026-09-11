using System.Runtime.InteropServices;
using System.Text;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace HEngine.Rendering.Managers;

public class DirectX12MeshShaderManager : IDisposable
{
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private readonly ShaderFileLoader _fileLoader;
    private readonly ShaderFileWatcher? _fileWatcher;
    private readonly ShaderDiskCache _diskCache;
    private readonly ShaderVariantCache _variantCache = new();
    private readonly ShaderVariantCompiler _variantCompiler = new();
    private readonly object _reloadLock = new();
    private readonly string _shaderFileName = "PBR.hlsl";

    private bool _disposed;
    private bool _isInitialized;
    private ComPtr<ID3D10Blob> _pixelShader;
    private ComPtr<ID3D10Blob> _vertexShader;
    private ShaderVariant _currentVariant;

    public event Action? ShaderReloaded;

    public ComPtr<ID3D10Blob> VertexShader => _vertexShader;
    public ComPtr<ID3D10Blob> PixelShader => _pixelShader;
    public ShaderVariant CurrentVariant => _currentVariant;

    public bool IsInitialized => _isInitialized && !_disposed;

    public DirectX12MeshShaderManager(
        ShaderFileLoader fileLoader,
        ShaderDiskCache diskCache,
        ShaderFileWatcher? fileWatcher = null)
    {
        _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
        _diskCache = diskCache ?? throw new ArgumentNullException(nameof(diskCache));
        _fileWatcher = fileWatcher;
        _currentVariant = new ShaderVariant(ShaderFeatureFlags.None);

        if (_fileWatcher != null)
        {
            _fileWatcher.ShaderFileChanged += OnShaderFileChanged;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_fileWatcher != null)
        {
            _fileWatcher.ShaderFileChanged -= OnShaderFileChanged;
        }

        _vertexShader.Dispose();
        _pixelShader.Dispose();
        _variantCache.Dispose();
        _variantCompiler.Dispose();
        _diskCache.Dispose();
        _fileLoader.Dispose();
        _compiler.Dispose();
        _isInitialized = false;
        _disposed = true;
    }

    public void Initialize()
    {
        Initialize(new ShaderVariant(ShaderFeatureFlags.None));
    }

    public void Initialize(ShaderVariant variant)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12MeshShaderManager));

        if (_isInitialized)
            return;

        _currentVariant = variant;
        LoadAndCompileShaders(variant);
        _isInitialized = true;
    }

    public void Reload()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12MeshShaderManager));

        if (!_isInitialized)
            throw new InvalidOperationException("Mesh shader manager must be initialized before reloading");

        lock (_reloadLock)
        {
            _variantCache.Clear();
            LoadAndCompileShaders(_currentVariant);
            ShaderReloaded?.Invoke();
        }
    }

    public void SetVariant(ShaderVariant variant)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12MeshShaderManager));

        if (!_isInitialized)
            throw new InvalidOperationException("Mesh shader manager must be initialized before changing variants");

        lock (_reloadLock)
        {
            _currentVariant = variant;

            if (_variantCache.TryGetVariant(variant, out var compiledVariant) && compiledVariant != null)
            {
                _vertexShader.Dispose();
                _pixelShader.Dispose();

                _vertexShader = compiledVariant.VertexShader;
                _pixelShader = compiledVariant.PixelShader;

                Console.WriteLine($"[ShaderVariant] Switched to cached variant: {variant.GetVariantName()}");
            }
            else
            {
                LoadAndCompileShaders(variant);
                Console.WriteLine($"[ShaderVariant] Compiled and switched to new variant: {variant.GetVariantName()}");
            }

            ShaderReloaded?.Invoke();
        }
    }

    public bool HasVariant(ShaderVariant variant)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12MeshShaderManager));

        return _variantCache.TryGetVariant(variant, out _);
    }

    public int GetCachedVariantCount()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12MeshShaderManager));

        return _variantCache.GetVariantCount();
    }

    private void OnShaderFileChanged(string fileName)
    {
        if (fileName != _shaderFileName)
            return;

        try
        {
            Console.WriteLine($"[ShaderHotReload] Detected change in {fileName}, reloading variant {_currentVariant.GetVariantName()}...");
            Reload();
            Console.WriteLine($"[ShaderHotReload] Successfully reloaded {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShaderHotReload] Failed to reload {fileName}: {ex.Message}");
        }
    }

    private void LoadAndCompileShaders(ShaderVariant variant)
    {
        ComPtr<ID3D10Blob> newVertexShader = default;
        ComPtr<ID3D10Blob> newPixelShader = default;

        try
        {
            var shaderCode = _fileLoader.LoadShaderCode(_shaderFileName);
            var shaderPath = _fileLoader.GetShaderPath(_shaderFileName);
            var variantKey = variant.GetVariantKey();

            bool vsFromCache = _diskCache.TryLoadCachedShader(
                shaderPath, shaderCode, "VSMain", "vs_5_0", variantKey, out newVertexShader);

            bool psFromCache = _diskCache.TryLoadCachedShader(
                shaderPath, shaderCode, "PSMain", "ps_5_0", variantKey, out newPixelShader);

            if (!vsFromCache)
            {
                newVertexShader = _variantCompiler.CompileShader(shaderCode, "VSMain", "vs_5_0", variant, _shaderFileName);
                _diskCache.SaveCachedShader(shaderPath, shaderCode, "VSMain", "vs_5_0", variantKey, newVertexShader);
            }

            if (!psFromCache)
            {
                newPixelShader = _variantCompiler.CompileShader(shaderCode, "PSMain", "ps_5_0", variant, _shaderFileName);
                _diskCache.SaveCachedShader(shaderPath, shaderCode, "PSMain", "ps_5_0", variantKey, newPixelShader);
            }

            _vertexShader.Dispose();
            _pixelShader.Dispose();

            _vertexShader = newVertexShader;
            _pixelShader = newPixelShader;

            _variantCache.AddVariant(variant, newVertexShader, newPixelShader);
        }
        catch
        {
            newVertexShader.Dispose();
            newPixelShader.Dispose();
            _isInitialized = false;
            throw;
        }
    }
}