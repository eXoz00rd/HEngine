cbuffer MeshConstants : register(b0)
{
    row_major float4x4 MVP;
    float4 LightDirection;
    float4 LightColor;
    float4 AmbientColor;
};

#ifdef USE_NORMAL_MAP
Texture2D NormalMap : register(t0);
SamplerState NormalSampler : register(s0);
#endif

#ifdef USE_SPECULAR_MAP
Texture2D SpecularMap : register(t1);
SamplerState SpecularSampler : register(s1);
#endif

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
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
#ifdef USE_NORMAL_MAP
    float3 Tangent : TANGENT;
    float3 Bitangent : BITANGENT;
#endif
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    output.Position = mul(float4(input.Position, 1.0), MVP);
    output.Normal = input.Normal;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
#ifdef USE_NORMAL_MAP
    output.Tangent = input.Tangent;
    output.Bitangent = input.Bitangent;
#endif
    return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    float3 normal = normalize(input.Normal);

#ifdef USE_NORMAL_MAP
    float3 tangentNormal = NormalMap.Sample(NormalSampler, input.TexCoord).xyz;
    tangentNormal = tangentNormal * 2.0 - 1.0;

    float3 T = normalize(input.Tangent);
    float3 B = normalize(input.Bitangent);
    float3 N = normalize(input.Normal);
    float3x3 TBN = float3x3(T, B, N);

    normal = normalize(mul(tangentNormal, TBN));
#endif

    float3 lightDir = normalize(-LightDirection.xyz);
    float diff = max(dot(normal, lightDir), 0.0);

    float3 ambient = AmbientColor.rgb * AmbientColor.a;
    float3 diffuse = diff * LightColor.rgb * LightColor.a;

#ifdef USE_SPECULAR_MAP
    float specularIntensity = SpecularMap.Sample(SpecularSampler, input.TexCoord).r;
    diffuse *= specularIntensity;
#endif

    float3 finalColor = (ambient + diffuse) * input.Color.rgb;
    return float4(finalColor, input.Color.a);
}