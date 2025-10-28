cbuffer MeshConstants : register(b0)
{
    float4x4 MVP;
    float4 LightDirection;
    float4 LightColor;
    float4 AmbientColor;
};

struct VS_INPUT
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    output.Position = mul(float4(input.Position, 1.0), MVP);
    output.Normal = input.Normal;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    float3 normal = normalize(input.Normal);
    float3 lightDir = normalize(-LightDirection.xyz);
    float diff = max(dot(normal, lightDir), 0.0);

    float3 ambient = AmbientColor.rgb * AmbientColor.a;
    float3 diffuse = diff * LightColor.rgb * LightColor.a;

    float3 finalColor = (ambient + diffuse) * input.Color.rgb;
    return float4(finalColor, input.Color.a);
}