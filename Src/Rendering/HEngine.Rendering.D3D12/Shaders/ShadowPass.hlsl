cbuffer ShadowPassConstants : register(b0)
{
    row_major float4x4 LightViewProjection;
    row_major float4x4 World;
};

struct VS_INPUT
{
    float3 Position : POSITION;
    float3 Normal   : NORMAL;
    float2 TexCoord : TEXCOORD;
    float4 Color    : COLOR;
};

float4 VSDepthOnly(VS_INPUT input) : SV_POSITION
{
    float4 worldPos = mul(float4(input.Position, 1.0), World);
    return mul(worldPos, LightViewProjection);
}

