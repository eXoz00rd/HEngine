using System.Numerics;
using System.Runtime.InteropServices;

namespace HEngine.Rendering.Data;

/// <summary>
/// GPU constant buffer b0 — per-object scene matrices and camera position.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PBRSceneConstants
{
    public Matrix4x4 World;
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 WorldViewProjection;
    public Matrix4x4 NormalMatrix;
    public Vector3 CameraPosition;
    public float Pad0;
}

/// <summary>
/// GPU constant buffer b1 — PBR material properties.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PBRMaterialConstants
{
    public Vector4 DiffuseColor;
    public float Metallic;
    public float Roughness;
    public float AO;
    public float EmissiveIntensity;
    public Vector4 EmissiveColor;
}

/// <summary>
/// GPU representation of a single light in the PBR light buffer.
/// Matches LightData struct in PBR.hlsl exactly (64 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PBRLightGpu
{
    public Vector3 Color;
    public float Intensity;
    public Vector3 Direction;
    public float Range;
    public Vector3 Position;
    public int Type;
    public float InnerConeAngle;
    public float OuterConeAngle;
    public Vector2 Pad;
}

/// <summary>
/// Layout constants for the PBR light constant buffer b2.
/// Used to write lights directly to the GPU buffer without a fixed-array struct.
/// </summary>
public static class PBRLightLayout
{
    public const int MaxLights = 8;
    public const int LightStructSize = 64;
    public const int LightsArraySize = MaxLights * LightStructSize;
    public const int ActiveCountOffset = LightsArraySize;
    public const int AmbientColorOffset = ActiveCountOffset + 4;
    public const int TotalSize = AmbientColorOffset + 12;
}

