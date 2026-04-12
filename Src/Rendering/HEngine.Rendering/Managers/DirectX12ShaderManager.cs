using System.Runtime.InteropServices;
using System.Text;
using HEngine.Core.Rendering.Contracts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace HEngine.Rendering.Managers;

public class DirectX12ShaderManager : IShaderManager, IDisposable
{
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private readonly ShaderFileLoader _fileLoader;
    private readonly ShaderDiskCache _diskCache;
    private readonly ShaderFileWatcher? _fileWatcher;
    private readonly object _reloadLock = new();
    private readonly string _shaderFileName = "Sprite.hlsl";

    private bool _disposed;
    private bool _isInitialized;
    private ComPtr<ID3D10Blob> _pixelShader;
    private ComPtr<ID3D10Blob> _vertexShader;

    public event Action? ShaderReloaded;

    public ComPtr<ID3D10Blob> VertexShader => _vertexShader;
    public ComPtr<ID3D10Blob> PixelShader => _pixelShader;

    public bool IsInitialized => _isInitialized && !_disposed;

    public DirectX12ShaderManager(
        ShaderFileLoader fileLoader,
        ShaderDiskCache diskCache,
        ShaderFileWatcher? fileWatcher = null)
    {
        _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
        _diskCache = diskCache ?? throw new ArgumentNullException(nameof(diskCache));
        _fileWatcher = fileWatcher;

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
        _diskCache.Dispose();
        _fileLoader.Dispose();
        _compiler.Dispose();
        _isInitialized = false;
        _disposed = true;
    }

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12ShaderManager));

        if (_isInitialized)
            return;

        LoadAndCompileShaders();
        _isInitialized = true;
    }

    public void Reload()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12ShaderManager));

        if (!_isInitialized)
            throw new InvalidOperationException("Shader manager must be initialized before reloading");

        lock (_reloadLock)
        {
            LoadAndCompileShaders();
            ShaderReloaded?.Invoke();
        }
    }

    private void OnShaderFileChanged(string fileName)
    {
        if (fileName != _shaderFileName)
            return;

        try
        {
            Console.WriteLine($"[ShaderHotReload] Detected change in {fileName}, reloading...");
            Reload();
            Console.WriteLine($"[ShaderHotReload] Successfully reloaded {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShaderHotReload] Failed to reload {fileName}: {ex.Message}");
        }
    }

    private void LoadAndCompileShaders()
    {
        ComPtr<ID3D10Blob> newVertexShader = default;
        ComPtr<ID3D10Blob> newPixelShader = default;

        try
        {
            var shaderCode = _fileLoader.LoadShaderCode(_shaderFileName);
            var shaderPath = _fileLoader.GetShaderPath(_shaderFileName);
            const string variantKey = "00000000";

            bool vsFromCache = _diskCache.TryLoadCachedShader(
                shaderPath, shaderCode, "VSMain", "vs_5_0", variantKey, out newVertexShader);

            bool psFromCache = _diskCache.TryLoadCachedShader(
                shaderPath, shaderCode, "PSMain", "ps_5_0", variantKey, out newPixelShader);

            if (!vsFromCache)
            {
                newVertexShader = CompileShader(shaderCode, "VSMain", "vs_5_0", _shaderFileName);
                _diskCache.SaveCachedShader(shaderPath, shaderCode, "VSMain", "vs_5_0", variantKey, newVertexShader);
            }

            if (!psFromCache)
            {
                newPixelShader = CompileShader(shaderCode, "PSMain", "ps_5_0", _shaderFileName);
                _diskCache.SaveCachedShader(shaderPath, shaderCode, "PSMain", "ps_5_0", variantKey, newPixelShader);
            }

            _vertexShader.Dispose();
            _pixelShader.Dispose();

            _vertexShader = newVertexShader;
            _pixelShader = newPixelShader;
        }
        catch
        {
            newVertexShader.Dispose();
            newPixelShader.Dispose();
            _isInitialized = false;
            throw;
        }
    }

    private ComPtr<ID3D10Blob> CompileShader(string shaderCode, string entryPoint, string target, string shaderFileName = "unknown")
    {
        var shaderBytes = Encoding.UTF8.GetBytes(shaderCode);
        var entryPointBytes = Encoding.UTF8.GetBytes(entryPoint);
        var targetBytes = Encoding.UTF8.GetBytes(target);

        unsafe
        {
            fixed (byte* shaderPtr = shaderBytes)
            fixed (byte* entryPointPtr = entryPointBytes)
            fixed (byte* targetPtr = targetBytes)
            {
                ID3D10Blob* shaderBlob = null;
                ID3D10Blob* errorBlob = null;
                
                var result = _compiler.Compile(
                    shaderPtr,
                    (nuint)shaderBytes.Length,
                    (byte*)null,
                    null,
                    null,
                    entryPointPtr,
                    targetPtr,
                    0u,
                    0u,
                    ref shaderBlob,
                    ref errorBlob);

                if (result < 0)
                {
                    var errorMessage = "Unknown shader compilation error";
                    if (errorBlob != null)
                    {
                        var errorPtr = errorBlob->GetBufferPointer();
                        var errorSize = errorBlob->GetBufferSize();
                        errorMessage = Marshal.PtrToStringAnsi((nint)errorPtr, (int)errorSize) ?? "Failed to get error message";
                        errorBlob->Release();
                    }

                    var detailedError = $"Shader compilation failed for '{shaderFileName}' (EntryPoint: {entryPoint}, Target: {target})\n" +
                                      $"Error Details:\n{errorMessage}";

                    throw new InvalidOperationException(detailedError);
                }

                if (errorBlob != null)
                    errorBlob->Release();

                return new ComPtr<ID3D10Blob>(shaderBlob);
            }
        }
    }
}