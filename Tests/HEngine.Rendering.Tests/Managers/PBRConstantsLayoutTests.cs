using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Rendering.Data;

namespace HEngine.Rendering.Tests.Managers;

public class PBRConstantsLayoutTests
{
    [Fact(DisplayName = "PBRSceneConstants struct has expected size (5 matrices + float3 + pad)")]
    public void PBRSceneConstants_Has_Expected_Size()
    {
        var size = Marshal.SizeOf<PBRSceneConstants>();
        Assert.Equal(5 * 64 + 16, size);
    }

    [Fact(DisplayName = "PBRMaterialConstants struct has expected size")]
    public void PBRMaterialConstants_Has_Expected_Size()
    {
        var size = Marshal.SizeOf<PBRMaterialConstants>();
        Assert.Equal(48, size);
    }

    [Fact(DisplayName = "PBRLightGpu struct has expected size of 64 bytes (matches HLSL LightData)")]
    public void PBRLightGpu_Has_Expected_Size()
    {
        var size = Marshal.SizeOf<PBRLightGpu>();
        Assert.Equal(64, size);
    }

    [Fact(DisplayName = "PBRLightLayout.TotalSize is 528 bytes (8 lights + int + float3)")]
    public void PBRLightLayout_TotalSize()
    {
        Assert.Equal(8 * 64, PBRLightLayout.LightsArraySize);
        Assert.Equal(PBRLightLayout.LightsArraySize + 4, PBRLightLayout.AmbientColorOffset);
        Assert.Equal(8 * 64 + 4 + 12, PBRLightLayout.TotalSize);
    }

    [Fact(DisplayName = "LightType enum has Spot variant")]
    public void LightType_Has_Spot()
    {
        Assert.Equal(0, (int)LightType.Directional);
        Assert.Equal(1, (int)LightType.Point);
        Assert.Equal(2, (int)LightType.Spot);
    }

    [Fact(DisplayName = "LightData contains InnerConeAngle and OuterConeAngle fields")]
    public void LightData_Has_Cone_Angles()
    {
        var ld = new LightData
        {
            Type = LightType.Spot,
            Color = Vector3.One,
            Intensity = 1f,
            InnerConeAngle = 0.5f,
            OuterConeAngle = 0.8f
        };

        Assert.Equal(0.5f, ld.InnerConeAngle);
        Assert.Equal(0.8f, ld.OuterConeAngle);
    }

    [Fact(DisplayName = "PBRSceneConstants fields map correctly")]
    public void PBRSceneConstants_Fields_Map_Correctly()
    {
        var sc = new PBRSceneConstants
        {
            World = Matrix4x4.Identity,
            View = Matrix4x4.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.UnitY),
            Projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 16f / 9f, 0.1f, 1000f),
            WorldViewProjection = Matrix4x4.Identity,
            NormalMatrix = Matrix4x4.Identity,
            CameraPosition = new Vector3(0, 0, -5),
            Pad0 = 0f
        };

        Assert.Equal(new Vector3(0, 0, -5), sc.CameraPosition);
        Assert.Equal(Matrix4x4.Identity, sc.World);
    }

    [Fact(DisplayName = "PBRMaterialConstants fields map correctly")]
    public void PBRMaterialConstants_Fields_Map_Correctly()
    {
        var mc = new PBRMaterialConstants
        {
            DiffuseColor = new Vector4(0.8f, 0.2f, 0.1f, 1f),
            Metallic = 1.0f,
            Roughness = 0.1f,
            AO = 0.9f,
            EmissiveIntensity = 2.5f,
            EmissiveColor = new Vector4(1f, 0.5f, 0f, 1f)
        };

        Assert.Equal(1.0f, mc.Metallic);
        Assert.Equal(0.1f, mc.Roughness);
        Assert.Equal(0.9f, mc.AO);
        Assert.Equal(2.5f, mc.EmissiveIntensity);
    }
}
