using System.Numerics;
using HEngine.Core.Components.Rendering;

namespace HEngine.Core.Mathematics;

/// <summary>
/// Provides utility methods for shadow mapping: PSSM splits, light-space VP matrices and pixel snapping.
/// </summary>
public static class ShadowUtils
{
    /// <summary>
    /// Computes PSSM (Parallel-Split Shadow Maps) cascade far distances using a log/linear blend.
    /// </summary>
    public static float[] ComputePSSMSplits(float near, float far, int count, float lambda)
    {
        if (count <= 0) return Array.Empty<float>();
        if (count == 1) return [far];

        var splits = new float[count];
        for (int i = 0; i < count; i++)
        {
            float p = (i + 1f) / count;
            float log = near * MathF.Pow(far / near, p);
            float lin = near + (far - near) * p;
            splits[i] = lambda * log + (1f - lambda) * lin;
        }
        splits[count - 1] = far;
        return splits;
    }

    /// <summary>
    /// Returns the 8 world-space corners of a sub-frustum slice (nearSplit..farSplit).
    /// </summary>
    public static Vector3[] GetFrustumCornersWorldSpace(in Camera camera, float nearSplit, float farSplit)
    {
        var proj = camera.IsOrthographic
            ? Matrix4x4.CreateOrthographic(
                camera.OrthographicSize * camera.AspectRatio,
                camera.OrthographicSize, nearSplit, farSplit)
            : Matrix4x4.CreatePerspectiveFieldOfView(
                camera.FieldOfView, camera.AspectRatio, nearSplit, farSplit);

        var view = Matrix4x4.CreateLookAt(camera.Position, camera.Target, camera.Up);
        var vp = view * proj;

        Matrix4x4.Invert(vp, out var invVP);

        ReadOnlySpan<Vector4> ndcCorners =
        [
            new(-1, -1, 0, 1),
            new( 1, -1, 0, 1),
            new(-1,  1, 0, 1),
            new( 1,  1, 0, 1),
            new(-1, -1, 1, 1),
            new( 1, -1, 1, 1),
            new(-1,  1, 1, 1),
            new( 1,  1, 1, 1),
        ];

        var corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            var worldH = Vector4.Transform(ndcCorners[i], invVP);
            corners[i] = new Vector3(worldH.X, worldH.Y, worldH.Z) / worldH.W;
        }
        return corners;
    }

    /// <summary>
    /// Computes a stable orthographic view-projection matrix for a directional light
    /// that tightly encloses the provided world-space frustum corners.
    /// </summary>
    public static Matrix4x4 ComputeDirectionalLightVP(Vector3 lightDir, ReadOnlySpan<Vector3> corners)
    {
        var dir = Vector3.Normalize(lightDir);

        var up = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) < 0.99f
            ? Vector3.UnitY
            : Vector3.UnitZ;

        var center = Vector3.Zero;
        foreach (var c in corners) center += c;
        center /= corners.Length;

        var lightView = Matrix4x4.CreateLookAt(center - dir * 100f, center, up);

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var c in corners)
        {
            var lc = Vector3.Transform(c, lightView);
            min = Vector3.Min(min, lc);
            max = Vector3.Max(max, lc);
        }

        var margin = 1.0f;
        min -= new Vector3(margin);
        max += new Vector3(margin);

        var lightProj = Matrix4x4.CreateOrthographicOffCenter(
            min.X, max.X, min.Y, max.Y, -max.Z - 200f, -min.Z + 200f);

        return lightView * lightProj;
    }

    /// <summary>
    /// Snaps a light-space VP matrix to the texel grid to eliminate shadow shimmering.
    /// </summary>
    public static Matrix4x4 SnapToTexelGrid(in Matrix4x4 lightVP, int resolution)
    {
        if (resolution <= 0) return lightVP;

        var shadowOrigin = Vector4.Transform(new Vector4(0f, 0f, 0f, 1f), lightVP);
        shadowOrigin *= resolution / 2f;

        var roundedOrigin = new Vector4(
            MathF.Round(shadowOrigin.X),
            MathF.Round(shadowOrigin.Y),
            shadowOrigin.Z,
            shadowOrigin.W);

        var roundOffset = (roundedOrigin - shadowOrigin) * (2f / resolution);

        var snapped = lightVP;
        snapped.M41 += roundOffset.X;
        snapped.M42 += roundOffset.Y;
        return snapped;
    }
}

