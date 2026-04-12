cbuffer CameraData : register(b0)
{
    row_major float4x4 View;
    row_major float4x4 Projection;
};

struct VS_INPUT
{
    float3 pos : POSITION;
    float4 color : COLOR;
};

struct VS_OUTPUT
{
    float4 pos : SV_POSITION;
    float4 color : COLOR;
};

VS_OUTPUT VSMain(VS_INPUT input)
{
    VS_OUTPUT output;

    float4 worldPos = float4(input.pos, 1.0f);
    float4 viewPos = mul(worldPos, View);
    float4 clipPos = mul(viewPos, Projection);
    output.pos = clipPos;
    output.color = input.color;
    return output;
}

struct PS_INPUT
{
    float4 pos : SV_POSITION;
    float4 color : COLOR;
};

float4 PSMain(PS_INPUT input) : SV_TARGET
{
    return input.color;
}
