cbuffer ToneMappingConstants : register(b0)
{
    int   ToneMappingMode;
    float Exposure;
    float Gamma;
    float _pad;
};

Texture2D    SourceTexture : register(t0);
SamplerState LinearSampler : register(s0);

struct VS_OUTPUT
{
    float4 Position  : SV_Position;
    float2 TexCoord  : TEXCOORD0;
};

VS_OUTPUT VSMain(uint id : SV_VertexID)
{
    VS_OUTPUT output;
    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
    return output;
}

float3 ACESFilmic(float3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

float3 Reinhard(float3 x)
{
    return x / (1.0 + x);
}

float3 Uncharted2Partial(float3 x)
{
    float A = 0.15;
    float B = 0.50;
    float C = 0.10;
    float D = 0.20;
    float E = 0.02;
    float F = 0.30;
    return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

float3 Uncharted2(float3 x)
{
    float W = 11.2;
    float3 curr = Uncharted2Partial(x * 2.0);
    float3 whiteScale = 1.0 / Uncharted2Partial(float3(W, W, W));
    return curr * whiteScale;
}

float3 GammaCorrect(float3 color, float gamma)
{
    return pow(max(color, 0.0001), 1.0 / gamma);
}

float4 PSMain(VS_OUTPUT input) : SV_Target
{
    float3 hdrColor = SourceTexture.Sample(LinearSampler, input.TexCoord).rgb;

    hdrColor *= Exposure;

    float3 mapped;
    if (ToneMappingMode == 1)
        mapped = Reinhard(hdrColor);
    else if (ToneMappingMode == 2)
        mapped = Uncharted2(hdrColor);
    else
        mapped = ACESFilmic(hdrColor);

    mapped = GammaCorrect(mapped, Gamma);

    return float4(mapped, 1.0);
}

