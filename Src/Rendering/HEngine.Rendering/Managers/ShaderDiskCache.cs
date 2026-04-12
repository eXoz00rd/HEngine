using System;
using System.IO;
using HEngine.Rendering.Data;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace HEngine.Rendering.Managers;

public sealed class ShaderDiskCache : IDisposable
{
    private readonly string _cacheDirectory;
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private bool _disposed;

    public ShaderDiskCache(string? cacheDirectory = null)
    {
        if (string.IsNullOrEmpty(cacheDirectory))
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectory = Path.Combine(basePath, "ShaderCache");
        }
        else
        {
            _cacheDirectory = cacheDirectory;
        }

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public bool TryLoadCachedShader(
        string shaderFilePath,
        string sourceCode,
        string entryPoint,
        string target,
        string variantKey,
        out ComPtr<ID3D10Blob> shaderBlob)
    {
        shaderBlob = default;

        try
        {
            var cacheKey = GenerateCacheKey(Path.GetFileName(shaderFilePath), entryPoint, target, variantKey);
            var binaryPath = Path.Combine(_cacheDirectory, $"{cacheKey}.bin");
            var metadataPath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");

            if (!File.Exists(binaryPath) || !File.Exists(metadataPath))
                return false;

            var metadataJson = File.ReadAllText(metadataPath);
            var metadata = ShaderCacheMetadata.FromJson(metadataJson);

            if (metadata == null)
                return false;

            var currentHash = ShaderCacheMetadata.ComputeHash(sourceCode);
            if (!metadata.IsValid(shaderFilePath, currentHash))
            {
                File.Delete(binaryPath);
                File.Delete(metadataPath);
                return false;
            }

            var bytecode = File.ReadAllBytes(binaryPath);
            shaderBlob = CreateBlobFromBytes(bytecode);

            Console.WriteLine($"[ShaderCache] Loaded cached shader: {Path.GetFileName(shaderFilePath)} ({entryPoint}, variant: {variantKey})");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShaderCache] Failed to load cached shader: {ex.Message}");
            return false;
        }
    }

    public void SaveCachedShader(
        string shaderFilePath,
        string sourceCode,
        string entryPoint,
        string target,
        string variantKey,
        ComPtr<ID3D10Blob> shaderBlob)
    {
        try
        {
            var cacheKey = GenerateCacheKey(Path.GetFileName(shaderFilePath), entryPoint, target, variantKey);
            var binaryPath = Path.Combine(_cacheDirectory, $"{cacheKey}.bin");
            var metadataPath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");

            unsafe
            {
                var ptr = shaderBlob.GetBufferPointer();
                var size = shaderBlob.GetBufferSize();
                var span = new ReadOnlySpan<byte>(ptr, (int)size);
                File.WriteAllBytes(binaryPath, span.ToArray());
            }

            var metadata = new ShaderCacheMetadata
            {
                SourceHash = ShaderCacheMetadata.ComputeHash(sourceCode),
                SourceTimestamp = File.GetLastWriteTimeUtc(shaderFilePath).Ticks,
                VariantKey = variantKey,
                EntryPoint = entryPoint,
                Target = target,
                ShaderFileName = Path.GetFileName(shaderFilePath),
                CacheVersion = 1
            };

            File.WriteAllText(metadataPath, metadata.ToJson());

            Console.WriteLine($"[ShaderCache] Saved shader to cache: {Path.GetFileName(shaderFilePath)} ({entryPoint}, variant: {variantKey})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShaderCache] Failed to save shader to cache: {ex.Message}");
        }
    }

    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                var files = Directory.GetFiles(_cacheDirectory);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                Console.WriteLine($"[ShaderCache] Cleared cache: {files.Length} files deleted");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShaderCache] Failed to clear cache: {ex.Message}");
        }
    }

    private string GenerateCacheKey(string fileName, string entryPoint, string target, string variantKey)
    {
        var combined = $"{fileName}_{entryPoint}_{target}_{variantKey}";
        var hash = ShaderCacheMetadata.ComputeHash(combined);
        return hash[..16];
    }

    private ComPtr<ID3D10Blob> CreateBlobFromBytes(byte[] bytecode)
    {
        unsafe
        {
            fixed (byte* dataPtr = bytecode)
            {
                ID3D10Blob* blob = null;
                var result = _compiler.CreateBlob((nuint)bytecode.Length, ref blob);

                if (result < 0)
                    throw new InvalidOperationException($"Failed to create blob. HRESULT: {result:X8}");

                var blobPtr = blob->GetBufferPointer();
                var blobSize = blob->GetBufferSize();

                new Span<byte>(blobPtr, (int)blobSize).Clear();
                new ReadOnlySpan<byte>(dataPtr, bytecode.Length).CopyTo(new Span<byte>(blobPtr, (int)blobSize));

                return new ComPtr<ID3D10Blob>(blob);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _compiler.Dispose();
        _disposed = true;
    }
}
