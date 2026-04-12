using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HEngine.Rendering.Data;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace HEngine.Rendering.Managers;

public sealed class ShaderVariantCompiler : IDisposable
{
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private bool _disposed;

    public ComPtr<ID3D10Blob> CompileShader(
        string shaderCode,
        string entryPoint,
        string target,
        ShaderVariant variant,
        string shaderFileName = "unknown")
    {
        var defines = variant.GetDefines();
        return CompileShaderWithDefines(shaderCode, entryPoint, target, defines, shaderFileName, variant.GetVariantName());
    }

    private ComPtr<ID3D10Blob> CompileShaderWithDefines(
        string shaderCode,
        string entryPoint,
        string target,
        Dictionary<string, string> defines,
        string shaderFileName,
        string variantName)
    {
        var shaderBytes = Encoding.UTF8.GetBytes(shaderCode);
        var entryPointBytes = Encoding.UTF8.GetBytes(entryPoint);
        var targetBytes = Encoding.UTF8.GetBytes(target);

        unsafe
        {
            D3DShaderMacro[]? macros = null;
            D3DShaderMacro* macrosPtr = null;

            if (defines.Count > 0)
            {
                macros = new D3DShaderMacro[defines.Count + 1];
                var index = 0;
                foreach (var kvp in defines)
                {
                    macros[index].Name = (byte*)Marshal.StringToHGlobalAnsi(kvp.Key);
                    macros[index].Definition = (byte*)Marshal.StringToHGlobalAnsi(kvp.Value);
                    index++;
                }
                macros[index].Name = null;
                macros[index].Definition = null;
            }

            try
            {
                fixed (byte* shaderPtr = shaderBytes)
                fixed (byte* entryPointPtr = entryPointBytes)
                fixed (byte* targetPtr = targetBytes)
                {
                    if (macros != null)
                    {
                        fixed (D3DShaderMacro* macroPtr = macros)
                        {
                            macrosPtr = macroPtr;
                        }
                    }

                    ID3D10Blob* shaderBlob = null;
                    ID3D10Blob* errorBlob = null;

                    var result = _compiler.Compile(
                        shaderPtr,
                        (nuint)shaderBytes.Length,
                        (byte*)null,
                        macrosPtr,
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

                        var detailedError = $"Shader compilation failed for '{shaderFileName}' (Variant: {variantName}, EntryPoint: {entryPoint}, Target: {target})\n" +
                                          $"Error Details:\n{errorMessage}";

                        throw new InvalidOperationException(detailedError);
                    }

                    if (errorBlob != null)
                        errorBlob->Release();

                    return new ComPtr<ID3D10Blob>(shaderBlob);
                }
            }
            finally
            {
                if (macros != null)
                {
                    for (int i = 0; i < macros.Length - 1; i++)
                    {
                        if (macros[i].Name != null)
                            Marshal.FreeHGlobal((nint)macros[i].Name);
                        if (macros[i].Definition != null)
                            Marshal.FreeHGlobal((nint)macros[i].Definition);
                    }
                }
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
