cbuffer FxaaConstants : register(b0)
{
    float FxaaQualitySubpix;
    float FxaaQualityEdgeThreshold;
    float FxaaQualityEdgeThresholdMin;
    float _pad;
    float2 RcpFrame;
    float2 _pad2;
};

Texture2D    SourceTexture : register(t0);
SamplerState LinearSampler : register(s0);

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

float FxaaLuma(float3 rgb)
{
    return rgb.y * (0.587 / 0.299) + rgb.x;
}

float4 PSMain(VS_OUTPUT input) : SV_Target
{
    float2 uv = input.TexCoord;
    float2 rcpFrame = RcpFrame;

    float3 rgbNW = SourceTexture.SampleLevel(LinearSampler, uv + float2(-1.0, -1.0) * rcpFrame, 0).rgb;
    float3 rgbNE = SourceTexture.SampleLevel(LinearSampler, uv + float2( 1.0, -1.0) * rcpFrame, 0).rgb;
    float3 rgbSW = SourceTexture.SampleLevel(LinearSampler, uv + float2(-1.0,  1.0) * rcpFrame, 0).rgb;
    float3 rgbSE = SourceTexture.SampleLevel(LinearSampler, uv + float2( 1.0,  1.0) * rcpFrame, 0).rgb;
    float3 rgbM  = SourceTexture.SampleLevel(LinearSampler, uv,                                  0).rgb;

    float lumaNW = FxaaLuma(rgbNW);
    float lumaNE = FxaaLuma(rgbNE);
    float lumaSW = FxaaLuma(rgbSW);
    float lumaSE = FxaaLuma(rgbSE);
    float lumaM  = FxaaLuma(rgbM);

    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    float lumaRange = lumaMax - lumaMin;
    if (lumaRange < max(FxaaQualityEdgeThresholdMin, lumaMax * FxaaQualityEdgeThreshold))
        return float4(rgbM, 1.0);

    float3 rgbL = rgbNW + rgbNE + rgbSW + rgbSE + rgbM;

    float2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float dirReduce = max(
        (lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * (1.0 / 8.0)),
        (1.0 / 128.0));

    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = min(float2( (1.0/4.0) / rcpFrame.x,  (1.0/4.0) / rcpFrame.y),
          max(float2(-(1.0/4.0) / rcpFrame.x, -(1.0/4.0) / rcpFrame.y),
              dir * rcpDirMin)) * rcpFrame;

    float3 rgbA = 0.5 * (
        SourceTexture.SampleLevel(LinearSampler, uv + dir * (1.0/3.0 - 0.5), 0).rgb +
        SourceTexture.SampleLevel(LinearSampler, uv + dir * (2.0/3.0 - 0.5), 0).rgb);

    float3 rgbB = rgbA * 0.5 + 0.25 * (
        SourceTexture.SampleLevel(LinearSampler, uv + dir * -0.5, 0).rgb +
        SourceTexture.SampleLevel(LinearSampler, uv + dir *  0.5, 0).rgb);

    float lumaB = FxaaLuma(rgbB);

    float3 finalColor;
    if ((lumaB < lumaMin) || (lumaB > lumaMax))
        finalColor = rgbA;
    else
        finalColor = rgbB;

    finalColor = lerp(rgbM, finalColor, FxaaQualitySubpix);

    return float4(finalColor, 1.0);
}

