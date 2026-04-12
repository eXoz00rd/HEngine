using System.Numerics;
using System.Runtime.InteropServices;

namespace HEngine.Rendering.Data;

/// <summary>
/// GPU constant buffer b3 — cascaded shadow map matrices and split distances.
/// Total size: 4 × 64 + 16 + 16 = 288 bytes (aligned to 16).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ShadowCbuffer
{
    public Matrix4x4 LightVP0;
    public Matrix4x4 LightVP1;
    public Matrix4x4 LightVP2;
    public Matrix4x4 LightVP3;

    public Vector4 CascadeSplits;

    public int CascadeCount;
    public float Pad0;
    public float Pad1;
    public float Pad2;

    public static ShadowCbuffer Create(
        ReadOnlySpan<Matrix4x4> lightVPs,
        ReadOnlySpan<float> splits)
    {
        var cb = new ShadowCbuffer();
        cb.CascadeCount = Math.Min(lightVPs.Length, 4);

        if (cb.CascadeCount > 0) cb.LightVP0 = lightVPs[0];
        if (cb.CascadeCount > 1) cb.LightVP1 = lightVPs[1];
        if (cb.CascadeCount > 2) cb.LightVP2 = lightVPs[2];
        if (cb.CascadeCount > 3) cb.LightVP3 = lightVPs[3];

        int sc = Math.Min(splits.Length, 4);
        cb.CascadeSplits = new Vector4(
            sc > 0 ? splits[0] : 0f,
            sc > 1 ? splits[1] : 0f,
            sc > 2 ? splits[2] : 0f,
            sc > 3 ? splits[3] : 0f);

        return cb;
    }
}

