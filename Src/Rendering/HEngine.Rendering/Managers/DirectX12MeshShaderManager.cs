using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace HEngine.Rendering.Managers;

public class DirectX12MeshShaderManager : IDisposable
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
            throw new ObjectDisposedException(nameof(DirectX12MeshShaderManager));

        if (_isInitialized)
            return;

        try
        {
            var shaderCode = GetMeshShaderCode();
            _vertexShader = CompileShader(shaderCode, "VSMain", "vs_5_0");
            _pixelShader = CompileShader(shaderCode, "PSMain", "ps_5_0");

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

    private string GetMeshShaderCode()
    {
        return @"
        cbuffer MeshConstants : register(b0)
        {
            row_major float4x4 MVP;
            float4 LightDirection;
            float4 LightColor;
            float4 AmbientColor;
        };

        struct VS_INPUT
        {
            float3 Position : POSITION;
            float3 Normal : NORMAL;
            float2 TexCoord : TEXCOORD;
            float4 Color : COLOR;
        };

        struct PS_INPUT
        {
            float4 Position : SV_POSITION;
            float3 Normal : NORMAL;
            float2 TexCoord : TEXCOORD;
            float4 Color : COLOR;
        };

        PS_INPUT VSMain(VS_INPUT input)
        {
            PS_INPUT output;
            output.Position = mul(float4(input.Position, 1.0), MVP);
            output.Normal = input.Normal;
            output.TexCoord = input.TexCoord;
            output.Color = input.Color;
            return output;
        }

        float4 PSMain(PS_INPUT input) : SV_TARGET
        {
            float3 normal = normalize(input.Normal);
            float3 lightDir = normalize(-LightDirection.xyz);
            float diff = max(dot(normal, lightDir), 0.0);

            float3 ambient = AmbientColor.rgb * AmbientColor.a;
            float3 diffuse = diff * LightColor.rgb * LightColor.a;

            float3 finalColor = (ambient + diffuse) * input.Color.rgb;
            return float4(finalColor, input.Color.a);
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

                    throw new Exception($"Mesh shader compilation failed: {errorMessage}");
                }

                if (errorBlob != null)
                    errorBlob->Release();

                return new ComPtr<ID3D10Blob>(shaderBlob);
            }
        }
    }
}