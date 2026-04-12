using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Mathematics;

namespace HEngine.Core.Tests.Math;

public class ShadowUtilsTests
{
    [Fact(DisplayName = "ComputePSSMSplits returns correct count")]
    public void ComputePSSMSplits_Returns_Correct_Count()
    {
        var splits = ShadowUtils.ComputePSSMSplits(0.1f, 200f, 4, 0.75f);
        Assert.Equal(4, splits.Length);
    }

    [Fact(DisplayName = "ComputePSSMSplits last split equals far plane")]
    public void ComputePSSMSplits_LastSplit_Equals_FarPlane()
    {
        const float far = 500f;
        var splits = ShadowUtils.ComputePSSMSplits(0.1f, far, 4, 0.75f);
        Assert.Equal(far, splits[^1], 3);
    }

    [Fact(DisplayName = "ComputePSSMSplits first split greater than near plane")]
    public void ComputePSSMSplits_FirstSplit_Greater_Than_Near()
    {
        const float near = 0.1f;
        var splits = ShadowUtils.ComputePSSMSplits(near, 200f, 4, 0.75f);
        Assert.True(splits[0] > near);
    }

    [Fact(DisplayName = "ComputePSSMSplits splits are monotonically increasing")]
    public void ComputePSSMSplits_Splits_Are_Monotonically_Increasing()
    {
        var splits = ShadowUtils.ComputePSSMSplits(0.1f, 200f, 4, 0.75f);
        for (int i = 1; i < splits.Length; i++)
            Assert.True(splits[i] > splits[i - 1]);
    }

    [Fact(DisplayName = "ComputePSSMSplits count 1 returns far plane")]
    public void ComputePSSMSplits_Count1_Returns_FarPlane()
    {
        const float far = 100f;
        var splits = ShadowUtils.ComputePSSMSplits(0.1f, far, 1, 0.75f);
        Assert.Single(splits);
        Assert.Equal(far, splits[0], 3);
    }

    [Fact(DisplayName = "ComputePSSMSplits count 0 returns empty")]
    public void ComputePSSMSplits_Count0_Returns_Empty()
    {
        var splits = ShadowUtils.ComputePSSMSplits(0.1f, 100f, 0, 0.75f);
        Assert.Empty(splits);
    }

    [Fact(DisplayName = "ComputeDirectionalLightVP produces invertible matrix")]
    public void ComputeDirectionalLightVP_Produces_Invertible_Matrix()
    {
        var corners = new Vector3[]
        {
            new(-10, -10, -10), new( 10, -10, -10),
            new(-10,  10, -10), new( 10,  10, -10),
            new(-10, -10,  10), new( 10, -10,  10),
            new(-10,  10,  10), new( 10,  10,  10),
        };

        var lightDir = Vector3.Normalize(new Vector3(-1f, -1f, -0.5f));
        var vp = ShadowUtils.ComputeDirectionalLightVP(lightDir, corners);
        Assert.True(Matrix4x4.Invert(vp, out _));
    }

    [Fact(DisplayName = "GetFrustumCornersWorldSpace returns 8 corners")]
    public void GetFrustumCornersWorldSpace_Returns_8_Corners()
    {
        var camera = new Camera(MathF.PI / 4f, 0.1f, 100f, 16f / 9f)
        {
            Position = new Vector3(0, 5, 10),
            Target = Vector3.Zero,
            Up = Vector3.UnitY
        };

        var corners = ShadowUtils.GetFrustumCornersWorldSpace(camera, 0.1f, 50f);
        Assert.Equal(8, corners.Length);
    }

    [Fact(DisplayName = "SnapToTexelGrid is stable for sub-texel translations")]
    public void SnapToTexelGrid_IsStable_For_SubTexelTranslation()
    {
        var corners = new Vector3[]
        {
            new(-5, -5, -5), new(5, -5, -5),
            new(-5,  5, -5), new(5,  5, -5),
            new(-5, -5,  5), new(5, -5,  5),
            new(-5,  5,  5), new(5,  5,  5),
        };
        var lightDir = Vector3.Normalize(new Vector3(0.3f, -1f, 0.2f));
        var vp = ShadowUtils.ComputeDirectionalLightVP(lightDir, corners);

        var snapped1 = ShadowUtils.SnapToTexelGrid(vp, 2048);
        var snapped2 = ShadowUtils.SnapToTexelGrid(vp, 2048);

        Assert.Equal(snapped1.M41, snapped2.M41, 5);
        Assert.Equal(snapped1.M42, snapped2.M42, 5);
    }
}

