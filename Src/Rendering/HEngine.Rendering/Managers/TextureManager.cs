using System.Collections.Concurrent;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Assets;
using HEngine.Rendering.Devices;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HEngine.Rendering.Managers;

/// <summary>
/// Manages GPU textures: loading, caching, ref-counting, and default textures.
/// Thread-safe. Works in headless mode (no GPU) for testing.
/// </summary>
public sealed class TextureManager : ITextureManager
{
    private readonly ILogger<TextureManager>? _logger;
    private readonly TextureLoader _textureLoader;
    private readonly DescriptorHeapManager? _descriptorHeapManager;
    private ComPtr<ID3D12Device> _device;
    private bool _hasGpuDevice;

    private readonly ConcurrentDictionary<string, int> _pathToHandle = new();
    private readonly ConcurrentDictionary<int, TextureEntry> _textures = new();
    private int _nextHandle;
    private bool _disposed;

    public int LoadedTextureCount => _textures.Count;

    public int DefaultWhiteTexture { get; private set; } = -1;
    public int DefaultNormalTexture { get; private set; } = -1;
    public int DefaultBlackTexture { get; private set; } = -1;

    /// <summary>
    /// Creates a TextureManager. If descriptorHeapManager is null, operates in headless/test mode.
    /// </summary>
    public TextureManager(
        TextureLoader? textureLoader = null,
        DescriptorHeapManager? descriptorHeapManager = null,
        ILogger<TextureManager>? logger = null)
    {
        _textureLoader = textureLoader ?? new TextureLoader();
        _descriptorHeapManager = descriptorHeapManager;
        _logger = logger;

        CreateDefaultTextures();
    }

    /// <summary>
    /// Provides a DX12 device for GPU resource creation. Call after device is initialized.
    /// Without this, TextureManager works in headless mode (CPU data only, no GPU textures).
    /// </summary>
    public void SetDevice(ComPtr<ID3D12Device> device)
    {
        _device = device;
        _hasGpuDevice = true;
        _logger?.LogInformation("TextureManager: GPU device set, GPU texture creation enabled.");

        if (_descriptorHeapManager is { IsInitialized: true })
        {
            foreach (var entry in _textures.Values)
            {
                if (entry.HasGpuResource)
                    continue;

                var srvHandle = _descriptorHeapManager.AllocateSrv();
                var gpuResource = CreateGpuTexture(entry.LoadResult, srvHandle);
                entry.AttachGpuResource(srvHandle, gpuResource);
            }
        }
    }

    public int LoadTexture(string filePath)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var normalizedPath = NormalizePath(filePath);

        if (_pathToHandle.TryGetValue(normalizedPath, out var existingHandle))
        {
            if (_textures.TryGetValue(existingHandle, out var existingEntry))
            {
                existingEntry.IncrementRef();
                return existingHandle;
            }
        }

        TextureLoadResult loadResult;
        try
        {
            loadResult = _textureLoader.Load(filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load texture: {Path}. Returning default white.", filePath);
            return DefaultWhiteTexture;
        }

        var handle = RegisterTexture(normalizedPath, loadResult);
        _logger?.LogInformation("Texture loaded: {Path} ({W}x{H}, handle={Handle})",
            normalizedPath, loadResult.Width, loadResult.Height, handle);
        return handle;
    }

    public async Task<int> LoadTextureAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var normalizedPath = NormalizePath(filePath);

        if (_pathToHandle.TryGetValue(normalizedPath, out var existingHandle))
        {
            if (_textures.TryGetValue(existingHandle, out var existingEntry))
            {
                existingEntry.IncrementRef();
                return existingHandle;
            }
        }

        TextureLoadResult loadResult;
        try
        {
            loadResult = await _textureLoader.LoadAsync(filePath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load texture async: {Path}. Returning default white.", filePath);
            return DefaultWhiteTexture;
        }

        return RegisterTexture(normalizedPath, loadResult);
    }

    public void ReleaseTexture(int textureHandle)
    {
        ThrowIfDisposed();

        if (!_textures.TryGetValue(textureHandle, out var entry))
            return;

        if (textureHandle == DefaultWhiteTexture ||
            textureHandle == DefaultNormalTexture ||
            textureHandle == DefaultBlackTexture)
            return;

        var remaining = entry.DecrementRef();

        if (remaining <= 0)
        {
            _textures.TryRemove(textureHandle, out _);
            if (!string.IsNullOrEmpty(entry.Path))
                _pathToHandle.TryRemove(entry.Path, out _);
            entry.Dispose();
        }
    }

    public bool IsTextureLoaded(int textureHandle)
    {
        return _textures.ContainsKey(textureHandle);
    }

    public int GetReferenceCount(int textureHandle)
    {
        return _textures.TryGetValue(textureHandle, out var entry) ? entry.RefCount : 0;
    }

    /// <summary>
    /// Gets the GPU resource for a loaded texture. Returns null in headless mode.
    /// </summary>
    public ComPtr<ID3D12Resource>? GetGpuResource(int textureHandle)
    {
        if (_textures.TryGetValue(textureHandle, out var entry) && entry.HasGpuResource)
            return entry.GpuResource;
        return null;
    }

    /// <summary>
    /// Gets the SRV descriptor handle for a loaded texture.
    /// </summary>
    public DescriptorHandle GetSrvHandle(int textureHandle)
    {
        if (_textures.TryGetValue(textureHandle, out var entry))
            return entry.SrvHandle;
        return DescriptorHandle.Invalid;
    }

    public unsafe void WriteSrvTo(int textureHandle, CpuDescriptorHandle destination)
    {
        if (!_textures.TryGetValue(textureHandle, out var entry) || !entry.HasGpuResource)
            return;

        var loadResult = entry.LoadResult;
        var mipLevels = loadResult.IsCompressed
            ? loadResult.MipLevels
            : CalculateMipLevels(loadResult.Width, loadResult.Height);

        var srvDesc = new ShaderResourceViewDesc
        {
            Format = loadResult.DxgiFormat,
            ViewDimension = SrvDimension.Texture2D,
            Shader4ComponentMapping = 0x00001688,
        };
        srvDesc.Texture2D.MipLevels = (uint)mipLevels;
        srvDesc.Texture2D.MostDetailedMip = 0;

        _device.CreateShaderResourceView(entry.GpuResource, in srvDesc, destination);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var entry in _textures.Values)
            entry.Dispose();

        _textures.Clear();
        _pathToHandle.Clear();
        _disposed = true;
    }

    private int RegisterTexture(string normalizedPath, TextureLoadResult loadResult)
    {
        var handle = Interlocked.Increment(ref _nextHandle);

        DescriptorHandle srvHandle = default;
        ComPtr<ID3D12Resource> gpuResource = default;

        if (_hasGpuDevice && _descriptorHeapManager is { IsInitialized: true })
        {
            srvHandle = _descriptorHeapManager.AllocateSrv();
            gpuResource = CreateGpuTexture(loadResult, srvHandle);
        }

        var entry = new TextureEntry(normalizedPath, loadResult, srvHandle, gpuResource);
        entry.IncrementRef();

        _textures[handle] = entry;
        _pathToHandle[normalizedPath] = handle;

        return handle;
    }

    /// <summary>
    /// Creates a DX12 GPU texture resource and SRV from a CPU-side TextureLoadResult.
    /// For uncompressed textures, generates mip levels CPU-side via box filter.
    /// </summary>
    private unsafe ComPtr<ID3D12Resource> CreateGpuTexture(TextureLoadResult loadResult, DescriptorHandle srvHandle)
    {
        int mipLevels = loadResult.IsCompressed
            ? loadResult.MipLevels
            : CalculateMipLevels(loadResult.Width, loadResult.Height);

        var texDesc = new ResourceDesc
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = (ulong)loadResult.Width,
            Height = (uint)loadResult.Height,
            DepthOrArraySize = 1,
            MipLevels = (ushort)mipLevels,
            Format = loadResult.DxgiFormat,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Layout = TextureLayout.LayoutUnknown,
            Flags = ResourceFlags.None
        };

        var defaultHeapProps = new HeapProperties
        {
            Type = HeapType.Default,
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown
        };

        var result = _device.CreateCommittedResource(
            in defaultHeapProps,
            HeapFlags.None,
            in texDesc,
            ResourceStates.CopyDest,
            null,
            out ComPtr<ID3D12Resource> texture);

        if (result < 0)
        {
            _logger?.LogError("Failed to create GPU texture ({W}x{H}). HRESULT: {HR:X8}",
                loadResult.Width, loadResult.Height, result);
            return default;
        }

        // Create SRV
        var srvDesc = new ShaderResourceViewDesc
        {
            Format = loadResult.DxgiFormat,
            ViewDimension = SrvDimension.Texture2D,
            Shader4ComponentMapping = 0x00001688, // D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };
        srvDesc.Texture2D.MipLevels = (uint)mipLevels;
        srvDesc.Texture2D.MostDetailedMip = 0;

        _device.CreateShaderResourceView(texture, in srvDesc, srvHandle.CpuHandle);

        _logger?.LogDebug("GPU texture created: {W}x{H}, {Mips} mips, format={Format}",
            loadResult.Width, loadResult.Height, mipLevels, loadResult.DxgiFormat);

        return texture;
    }

    /// <summary>
    /// Generates CPU-side mipmap chain using a simple box filter (2x2 averaging).
    /// Returns all mip levels as a flat byte array.
    /// </summary>
    public static byte[] GenerateMipmapsCpu(byte[] sourcePixels, int width, int height, int bytesPerPixel, out int[] mipOffsets, out int totalMipLevels)
    {
        totalMipLevels = CalculateMipLevels(width, height);
        mipOffsets = new int[totalMipLevels];

        // Calculate total size
        int totalSize = 0;
        int w = width, h = height;
        for (int m = 0; m < totalMipLevels; m++)
        {
            mipOffsets[m] = totalSize;
            totalSize += w * h * bytesPerPixel;
            w = Math.Max(1, w / 2);
            h = Math.Max(1, h / 2);
        }

        var result = new byte[totalSize];

        // Copy mip 0 (original)
        Buffer.BlockCopy(sourcePixels, 0, result, 0, Math.Min(sourcePixels.Length, width * height * bytesPerPixel));

        // Generate subsequent mip levels via box filter
        int srcW = width, srcH = height;
        for (int m = 1; m < totalMipLevels; m++)
        {
            int dstW = Math.Max(1, srcW / 2);
            int dstH = Math.Max(1, srcH / 2);

            int srcOffset = mipOffsets[m - 1];
            int dstOffset = mipOffsets[m];

            for (int y = 0; y < dstH; y++)
            {
                for (int x = 0; x < dstW; x++)
                {
                    int sx = x * 2;
                    int sy = y * 2;

                    for (int c = 0; c < bytesPerPixel; c++)
                    {
                        int s00 = result[srcOffset + (sy * srcW + sx) * bytesPerPixel + c];
                        int s10 = result[srcOffset + (sy * srcW + Math.Min(sx + 1, srcW - 1)) * bytesPerPixel + c];
                        int s01 = result[srcOffset + (Math.Min(sy + 1, srcH - 1) * srcW + sx) * bytesPerPixel + c];
                        int s11 = result[srcOffset + (Math.Min(sy + 1, srcH - 1) * srcW + Math.Min(sx + 1, srcW - 1)) * bytesPerPixel + c];

                        result[dstOffset + (y * dstW + x) * bytesPerPixel + c] = (byte)((s00 + s10 + s01 + s11 + 2) / 4);
                    }
                }
            }

            srcW = dstW;
            srcH = dstH;
        }

        return result;
    }

    /// <summary>
    /// Calculates the number of mip levels for a given texture dimension.
    /// </summary>
    public static int CalculateMipLevels(int width, int height)
    {
        int levels = 1;
        int dim = Math.Max(width, height);
        while (dim > 1)
        {
            dim /= 2;
            levels++;
        }
        return levels;
    }

    private void CreateDefaultTextures()
    {
        DefaultWhiteTexture = CreateDefaultTexture("__default_white",
            [255, 255, 255, 255]);
        DefaultNormalTexture = CreateDefaultTexture("__default_normal",
            [128, 128, 255, 255]);
        DefaultBlackTexture = CreateDefaultTexture("__default_black",
            [0, 0, 0, 255]);
    }

    private int CreateDefaultTexture(string name, byte[] pixelData)
    {
        var loadResult = new TextureLoadResult(
            pixelData: pixelData,
            width: 1,
            height: 1,
            mipLevels: 1,
            dxgiFormat: Format.FormatR8G8B8A8Unorm,
            bytesPerPixel: 4,
            isCompressed: false,
            sourcePath: name);
        return RegisterTexture(name, loadResult);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static string NormalizePath(string path)
    {
        if (path.StartsWith("__")) return path; // internal names
        return Path.GetFullPath(path).ToLowerInvariant();
    }

    private sealed class TextureEntry : IDisposable
    {
        private int _refCount;

        public string Path { get; }
        public TextureLoadResult LoadResult { get; }
        public DescriptorHandle SrvHandle { get; private set; }
        public ComPtr<ID3D12Resource> GpuResource { get; private set; }
        public bool HasGpuResource { get; private set; }
        public int RefCount => _refCount;

        public TextureEntry(string path, TextureLoadResult loadResult, DescriptorHandle srvHandle, ComPtr<ID3D12Resource> gpuResource = default)
        {
            Path = path;
            LoadResult = loadResult;
            SrvHandle = srvHandle;
            GpuResource = gpuResource;
            unsafe { HasGpuResource = gpuResource.Handle != null; }
        }

        public void AttachGpuResource(DescriptorHandle srvHandle, ComPtr<ID3D12Resource> gpuResource)
        {
            SrvHandle = srvHandle;
            GpuResource = gpuResource;
            unsafe { HasGpuResource = gpuResource.Handle != null; }
        }

        public int IncrementRef() => Interlocked.Increment(ref _refCount);
        public int DecrementRef() => Interlocked.Decrement(ref _refCount);

        public void Dispose()
        {
            if (HasGpuResource)
                GpuResource.Dispose();
            LoadResult.Dispose();
        }
    }
}

