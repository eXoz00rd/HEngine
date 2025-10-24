using System.Runtime.InteropServices;
using System.Text;
using HEngine.Core.Rendering.Contracts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace HEngine.Rendering.Managers;

public class DirectX12ShaderManager : IShaderManager, IDisposable
{
    private readonly D3DCompiler _compiler = D3DCompiler.GetApi();
    private bool _disposed;
    private bool _isInitialized;
    private ComPtr<ID3D10Blob> _pixelShader;
    private ComPtr<ID3D10Blob> _vertexShader;

    public ComPtr<ID3D10Blob> VertexShader => _vertexShader;
    public ComPtr<ID3D10Blob> PixelShader => _pixelShader;
    
    public bool IsInitialized => _isInitialized && !_disposed;

    public void Dispose()
    {
        if (_disposed) return;

        _vertexShader.Dispose();
        _pixelShader.Dispose();
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

        try
        {
            _vertexShader = CompileShader(GetVertexShaderCode(), "main", "vs_5_0");
            _pixelShader = CompileShader(GetPixelShaderCode(), "main", "ps_5_0");

            _isInitialized = true;
        }
        catch
        {
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _isInitialized = false;
            throw;
        }
    }

    private string GetVertexShaderCode()
    {
        return @"
        struct VS_INPUT {
            float3 pos : POSITION;
            float4 color : COLOR;
        };
        
        struct VS_OUTPUT {
            float4 pos : SV_POSITION;
            float4 color : COLOR;
        };
        
        cbuffer CameraData : register(b0) {
            row_major float4x4 View;
            row_major float4x4 Projection;
        };
        
        VS_OUTPUT main(VS_INPUT input) {
            VS_OUTPUT output;
            
            float4 worldPos = float4(input.pos, 1.0f);
            float4 viewPos = mul(worldPos, View);
            float4 clipPos = mul(viewPos, Projection);
            output.pos = clipPos;
            output.color = input.color;
            return output;
        }";
    }

    private string GetPixelShaderCode()
    {
        return @"
        struct PS_INPUT {
            float4 pos : SV_POSITION;
            float4 color : COLOR;
        };
        
        float4 main(PS_INPUT input) : SV_TARGET {
            return input.color;
        }";
    }

    private ComPtr<ID3D10Blob> CompileShader(string shaderCode, string entryPoint, string target)
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
                        errorMessage = Marshal.PtrToStringAnsi((nint)errorPtr) ?? "Failed to get error message";
                        errorBlob->Release();
                    }

                    throw new Exception($"Shader compilation failed: {errorMessage}");
                }

                if (errorBlob != null)
                    errorBlob->Release();

                return new ComPtr<ID3D10Blob>(shaderBlob);
            }
        }
    }
}