using System.Runtime.InteropServices;

namespace HEngine.Rendering.Data;

/// <summary>
/// GPU root constants (b0) for the ToneMapping fullscreen pass — matches
/// <c>cbuffer ToneMappingConstants</c> in ToneMapping.hlsl. 16 bytes = 4 root 32-bit values.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ToneMappingCbuffer
{
    public int ToneMappingMode;
    public float Exposure;
    public float Gamma;
    public float Pad;

    public static ToneMappingCbuffer Create(int toneMappingMode, float exposure, float gamma)
    {
        return new ToneMappingCbuffer
        {
            ToneMappingMode = toneMappingMode,
            Exposure = exposure,
            Gamma = gamma,
            Pad = 0f
        };
    }
}
