using System;
using System.Collections.Concurrent;
using HEngine.Rendering.Data;
using Silk.NET.Core.Native;

namespace HEngine.Rendering.Managers;

public sealed class ShaderVariantCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CompiledShaderVariant> _cache = new();
    private bool _disposed;

    public bool TryGetVariant(ShaderVariant variant, out CompiledShaderVariant? compiledVariant)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var key = variant.GetVariantKey();
        return _cache.TryGetValue(key, out compiledVariant);
    }

    public void AddVariant(ShaderVariant variant, ComPtr<ID3D10Blob> vertexShader, ComPtr<ID3D10Blob> pixelShader)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = variant.GetVariantKey();
        var compiledVariant = new CompiledShaderVariant(variant, vertexShader, pixelShader);

        _cache.AddOrUpdate(key, compiledVariant, (_, oldVariant) =>
        {
            oldVariant.Dispose();
            return compiledVariant;
        });
    }

    public bool RemoveVariant(ShaderVariant variant)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = variant.GetVariantKey();
        if (_cache.TryRemove(key, out var compiledVariant))
        {
            compiledVariant.Dispose();
            return true;
        }

        return false;
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var variant in _cache.Values)
        {
            variant.Dispose();
        }

        _cache.Clear();
    }

    public int GetVariantCount()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cache.Count;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var variant in _cache.Values)
        {
            variant.Dispose();
        }

        _cache.Clear();
        _disposed = true;
    }
}

public sealed class CompiledShaderVariant : IDisposable
{
    private bool _disposed;

    public ShaderVariant Variant { get; }
    public ComPtr<ID3D10Blob> VertexShader { get; private set; }
    public ComPtr<ID3D10Blob> PixelShader { get; private set; }

    public CompiledShaderVariant(ShaderVariant variant, ComPtr<ID3D10Blob> vertexShader, ComPtr<ID3D10Blob> pixelShader)
    {
        Variant = variant;
        VertexShader = vertexShader;
        PixelShader = pixelShader;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        VertexShader.Dispose();
        PixelShader.Dispose();
        _disposed = true;
    }
}
