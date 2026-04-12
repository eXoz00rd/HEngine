using System;
using System.IO;

namespace HEngine.Rendering.Managers;

public sealed class ShaderFileLoader : IDisposable
{
    private readonly string _shaderDirectory;
    private bool _disposed;

    public ShaderFileLoader(string? customShaderDirectory = null)
    {
        if (string.IsNullOrEmpty(customShaderDirectory))
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            _shaderDirectory = Path.Combine(basePath, "Shaders");
        }
        else
        {
            _shaderDirectory = customShaderDirectory;
        }

        if (!Directory.Exists(_shaderDirectory))
        {
            throw new DirectoryNotFoundException($"Shader directory not found: {_shaderDirectory}");
        }
    }

    public string LoadShaderCode(string shaderFileName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var shaderPath = Path.Combine(_shaderDirectory, shaderFileName);

        if (!File.Exists(shaderPath))
        {
            throw new FileNotFoundException($"Shader file not found: {shaderPath}", shaderPath);
        }

        try
        {
            return File.ReadAllText(shaderPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read shader file: {shaderPath}", ex);
        }
    }

    public string GetShaderPath(string shaderFileName)
    {
        return Path.Combine(_shaderDirectory, shaderFileName);
    }

    public bool ShaderFileExists(string shaderFileName)
    {
        var shaderPath = Path.Combine(_shaderDirectory, shaderFileName);
        return File.Exists(shaderPath);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
