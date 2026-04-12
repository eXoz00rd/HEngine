#define MAX_LIGHTS 8
#define PI 3.14159265359
#define EPSILON 0.00001

cbuffer PBRConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 View;
    row_major float4x4 Projection;
    row_major float4x4 WorldViewProjection;
    row_major float4x4 NormalMatrix;
    float3 CameraPosition;
    float _pad0;
};

cbuffer MaterialConstants : register(b1)
{
    float4 DiffuseColor;
    float Metallic;
    float Roughness;
    float AO;
    float EmissiveIntensity;
    float4 EmissiveColor;
};

struct LightData
{
    float3 Color;
    float Intensity;
    float3 Direction;
    float Range;
    float3 Position;
    int Type;
    float InnerConeAngle;
    float OuterConeAngle;
    float2 _pad;
};

cbuffer LightConstants : register(b2)
{
    LightData Lights[MAX_LIGHTS];
    int ActiveLightCount;
    float3 AmbientColor;
};

Texture2D AlbedoMap : register(t0);
Texture2D NormalMap : register(t1);
Texture2D MetallicRoughnessMap : register(t2);
Texture2D EmissiveMap : register(t3);
Texture2D AOMap : register(t4);

#ifdef USE_SHADOWS
Texture2DArray ShadowMap : register(t5);
SamplerComparisonState ShadowSampler : register(s1);

cbuffer ShadowConstants : register(b3)
{
    row_major float4x4 LightVP[4];
    float4 CascadeSplits;
    int CascadeCount;
    float _shadowPad0;
    float _shadowPad1;
    float _shadowPad2;
};

float SampleShadowPCF(float3 worldPos, int cascade)
{
    float4 lightClip = mul(float4(worldPos, 1.0), LightVP[cascade]);
    float3 projCoords = lightClip.xyz / lightClip.w;

    float2 uv = projCoords.xy * float2(0.5, -0.5) + 0.5;

    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
        return 1.0;

    float depth = projCoords.z;
    float shadowBias = 0.001;

    float shadow = 0.0;
    float texelSize = 1.0 / 2048.0;

    [unroll]
    for (int x = -1; x <= 1; x++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            float2 offset = float2(x, y) * texelSize;
            shadow += ShadowMap.SampleCmpLevelZero(
                ShadowSampler,
                float3(uv + offset, (float)cascade),
                depth - shadowBias);
        }
    }

    return shadow / 9.0;
}

float ComputeShadowFactor(float3 worldPos, float viewDepth)
{
    int cascade = CascadeCount - 1;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        if (i < CascadeCount && viewDepth < CascadeSplits[i])
        {
            cascade = i;
            break;
        }
    }
    return SampleShadowPCF(worldPos, cascade);
}
#endif

SamplerState LinearSampler : register(s0);

struct VS_INPUT
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
#ifdef USE_NORMAL_MAP
    float3 Tangent : TANGENT;
    float3 Bitangent : BITANGENT;
#endif
};

struct PS_INPUT
{
    float4 ClipPosition : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD1;
    float4 Color : COLOR;
#ifdef USE_NORMAL_MAP
    float3 Tangent : TEXCOORD2;
    float3 Bitangent : TEXCOORD3;
#endif
    float ViewDepth : TEXCOORD4;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    output.ClipPosition = mul(float4(input.Position, 1.0), WorldViewProjection);
    output.WorldPosition = mul(float4(input.Position, 1.0), World).xyz;
    output.Normal = normalize(mul(float4(input.Normal, 0.0), NormalMatrix).xyz);
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    output.ViewDepth = output.ClipPosition.w;
#ifdef USE_NORMAL_MAP
    output.Tangent = normalize(mul(float4(input.Tangent, 0.0), World).xyz);
    output.Bitangent = normalize(mul(float4(input.Bitangent, 0.0), World).xyz);
#endif
    return output;
}

float DistributionGGX(float3 N, float3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float denom = (NdotH * NdotH * (a2 - 1.0) + 1.0);
    return a2 / (PI * denom * denom + EPSILON);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotV / (NdotV * (1.0 - k) + k + EPSILON);
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
}

float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float3 EvaluatePBR(float3 albedo, float metallic, float roughness, float ao,
                   float3 N, float3 V, float3 L, float3 lightColor, float attenuation)
{
    float3 H = normalize(V + L);

    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);

    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    float3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    float3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + EPSILON;
    float3 specular = numerator / denominator;

    float3 kS = F;
    float3 kD = (1.0 - kS) * (1.0 - metallic);

    float NdotL = max(dot(N, L), 0.0);
    return (kD * albedo / PI + specular) * lightColor * attenuation * NdotL;
}

float3 ComputeLightContribution(float3 worldPos, float3 N, float3 V,
                                 float3 albedo, float metallic, float roughness, float ao,
                                 LightData light)
{
    float3 L;
    float attenuation;
    float3 lightColor = light.Color * light.Intensity;

    if (light.Type == 0)
    {
        L = normalize(-light.Direction);
        attenuation = 1.0;
    }
    else if (light.Type == 1)
    {
        float3 delta = light.Position - worldPos;
        float dist = length(delta);
        L = normalize(delta);
        float distAttenuation = 1.0 / (dist * dist + EPSILON);
        float rangeAttenuation = pow(clamp(1.0 - pow(dist / max(light.Range, EPSILON), 4.0), 0.0, 1.0), 2.0);
        attenuation = distAttenuation * rangeAttenuation;
    }
    else
    {
        float3 delta = light.Position - worldPos;
        float dist = length(delta);
        L = normalize(delta);
        float distAttenuation = 1.0 / (dist * dist + EPSILON);
        float rangeAttenuation = pow(clamp(1.0 - pow(dist / max(light.Range, EPSILON), 4.0), 0.0, 1.0), 2.0);
        float cosAngle = dot(normalize(-light.Direction), L);
        float spotAttenuation = smoothstep(cos(light.OuterConeAngle), cos(light.InnerConeAngle), cosAngle);
        attenuation = distAttenuation * rangeAttenuation * spotAttenuation;
    }

    return EvaluatePBR(albedo, metallic, roughness, ao, N, V, L, lightColor, attenuation);
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    float4 albedoSample = DiffuseColor;
#ifdef USE_ALBEDO_MAP
    albedoSample *= AlbedoMap.Sample(LinearSampler, input.TexCoord);
#endif
    albedoSample *= input.Color;
    float3 albedo = albedoSample.rgb;

    float metallic = Metallic;
    float roughness = Roughness;
#ifdef USE_METALLIC_ROUGHNESS
    float2 mrSample = MetallicRoughnessMap.Sample(LinearSampler, input.TexCoord).bg;
    metallic = mrSample.x;
    roughness = mrSample.y;
#endif

    float ao = AO;
#ifdef USE_AO_MAP
    ao = AOMap.Sample(LinearSampler, input.TexCoord).r;
#endif

    float3 N = normalize(input.Normal);
#ifdef USE_NORMAL_MAP
    float3 tangentNormal = NormalMap.Sample(LinearSampler, input.TexCoord).xyz;
    tangentNormal = tangentNormal * 2.0 - 1.0;
    float3 T = normalize(input.Tangent);
    float3 B = normalize(input.Bitangent);
    float3x3 TBN = float3x3(T, B, N);
    N = normalize(mul(tangentNormal, TBN));
#endif

    float3 V = normalize(CameraPosition - input.WorldPosition);

    float3 Lo = float3(0.0, 0.0, 0.0);
    for (int i = 0; i < ActiveLightCount && i < MAX_LIGHTS; i++)
    {
        float3 contrib = ComputeLightContribution(input.WorldPosition, N, V, albedo, metallic, roughness, ao, Lights[i]);

#ifdef USE_SHADOWS
        if (Lights[i].Type == 0)
        {
            float shadowFactor = ComputeShadowFactor(input.WorldPosition, input.ViewDepth);
            contrib *= shadowFactor;
        }
#endif

        Lo += contrib;
    }

    float3 ambient = AmbientColor * albedo * ao;
    float3 color = ambient + Lo;

    float3 emissive = EmissiveColor.rgb * EmissiveIntensity;
#ifdef USE_EMISSIVE_MAP
    emissive *= EmissiveMap.Sample(LinearSampler, input.TexCoord).rgb;
#endif
    color += emissive;

    return float4(color, albedoSample.a);
}

