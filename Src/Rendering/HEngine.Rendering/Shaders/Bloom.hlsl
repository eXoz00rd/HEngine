cbuffer BloomConstants : register(b0)
{
    float BloomThreshold;
    float BloomIntensity;
    float BloomRadius;
    int   BloomPass;
    float TexelSizeX;
    float TexelSizeY;
    int   MipLevel;
    float _pad;
};

Texture2D    SourceTexture   : register(t0);
Texture2D    BloomTexture    : register(t1);
SamplerState LinearSampler   : register(s0);

struct VS_OUTPUT
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

VS_OUTPUT VSMain(uint id : SV_VertexID)
{
    VS_OUTPUT output;
    output.TexCoord = float2((id << 1) & 2, id & 2);
    output.Position = float4(output.TexCoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
    return output;
}

#define BLOOM_PASS_THRESHOLD  0
#define BLOOM_PASS_DOWNSAMPLE 1
#define BLOOM_PASS_BLUR       2
#define BLOOM_PASS_UPSAMPLE   3
#define BLOOM_PASS_COMPOSITE  4

float3 BrightnessThreshold(float2 uv)
{
    float3 color = SourceTexture.Sample(LinearSampler, uv).rgb;
    float brightness = dot(color, float3(0.2126, 0.7152, 0.0722));
    float knee = BloomThreshold * 0.5;
    float rq = clamp(brightness - BloomThreshold + knee, 0.0, 2.0 * knee);
    rq = (rq * rq) / (4.0 * knee + 0.00001);
    float weight = max(rq, brightness - BloomThreshold) / max(brightness, 0.00001);
    return color * weight;
}

float3 DownsampleBox(Texture2D tex, float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
    float3 s;
    s  = tex.Sample(LinearSampler, uv + d.xy).rgb;
    s += tex.Sample(LinearSampler, uv + d.zy).rgb;
    s += tex.Sample(LinearSampler, uv + d.xw).rgb;
    s += tex.Sample(LinearSampler, uv + d.zw).rgb;
    return s * 0.25;
}

float3 GaussianBlur(Texture2D tex, float2 uv, float2 dir, float2 texelSize)
{
    float3 result = float3(0.0, 0.0, 0.0);
    float offsets[5] = {-2.0, -1.0, 0.0, 1.0, 2.0};
    float weights[5] = {0.0625, 0.25, 0.375, 0.25, 0.0625};

    for (int i = 0; i < 5; i++)
    {
        float2 offset = dir * offsets[i] * texelSize;
        result += tex.Sample(LinearSampler, uv + offset).rgb * weights[i];
    }
    return result;
}

float3 UpsampleTent(Texture2D tex, float2 uv, float2 texelSize, float radius)
{
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * radius;
    float3 s;
    s  = tex.Sample(LinearSampler, uv - d.xy).rgb;
    s += tex.Sample(LinearSampler, uv - d.wy).rgb * 2.0;
    s += tex.Sample(LinearSampler, uv + d.zy).rgb;
    s += tex.Sample(LinearSampler, uv - d.xw).rgb * 2.0;
    s += tex.Sample(LinearSampler, uv        ).rgb * 4.0;
    s += tex.Sample(LinearSampler, uv + d.xw).rgb * 2.0;
    s += tex.Sample(LinearSampler, uv + d.xy).rgb;
    s += tex.Sample(LinearSampler, uv + d.wy).rgb * 2.0;
    s += tex.Sample(LinearSampler, uv - d.zy).rgb;
    return s / 16.0;
}

float4 PSMain(VS_OUTPUT input) : SV_Target
{
    float2 texelSize = float2(TexelSizeX, TexelSizeY);

    if (BloomPass == BLOOM_PASS_THRESHOLD)
    {
        return float4(BrightnessThreshold(input.TexCoord), 1.0);
    }
    else if (BloomPass == BLOOM_PASS_DOWNSAMPLE)
    {
        return float4(DownsampleBox(SourceTexture, input.TexCoord, texelSize), 1.0);
    }
    else if (BloomPass == BLOOM_PASS_BLUR)
    {
        float2 hDir = float2(1.0, 0.0);
        float2 vDir = float2(0.0, 1.0);
        float3 horiz = GaussianBlur(SourceTexture, input.TexCoord, hDir, texelSize);
        float3 vert  = GaussianBlur(SourceTexture, input.TexCoord, vDir, texelSize);
        return float4((horiz + vert) * 0.5, 1.0);
    }
    else if (BloomPass == BLOOM_PASS_UPSAMPLE)
    {
        return float4(UpsampleTent(BloomTexture, input.TexCoord, texelSize, BloomRadius), 1.0);
    }
    else
    {
        float3 scene = SourceTexture.Sample(LinearSampler, input.TexCoord).rgb;
        float3 bloom = BloomTexture.Sample(LinearSampler, input.TexCoord).rgb;
        return float4(scene + bloom * BloomIntensity, 1.0);
    }
}

