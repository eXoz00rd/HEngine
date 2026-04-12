using System;

namespace HEngine.Rendering.Enums;

[Flags]
public enum ShaderFeatureFlags : uint
{
    None = 0,
    UseNormalMap = 1 << 0,
    UseSpecularMap = 1 << 1,
    UseEmissiveMap = 1 << 2,
    UseAlphaTest = 1 << 3,
    UseSkinning = 1 << 4,
    UseVertexColors = 1 << 5,
    UseInstancing = 1 << 6,
    UseFog = 1 << 7,
    UseShadows = 1 << 8,
    UseAmbientOcclusion = 1 << 9,
    UseMetallicRoughness = 1 << 10,
    UseParallaxMapping = 1 << 11
}
