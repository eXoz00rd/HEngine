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

float4 PSMain(VS_OUTPUT input) : SV_Target
{
    return float4(SourceTexture.Sample(LinearSampler, input.TexCoord).rgb, 1.0);
}
