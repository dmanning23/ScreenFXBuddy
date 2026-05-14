#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SceneTexture;
sampler2D SceneSampler = sampler_state
{
    Texture   = <SceneTexture>;
    AddressU  = Clamp;
    AddressV  = Clamp;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
};

float  VortexCount;
float4 VortexOrigins[4];   // .xy = (originX_uv, originY_uv)
float4 VortexState[4];     // .x = swirl (signed, pre-faded), .y = radius
float  AspectRatio;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;
    float2 totalDisplacement = float2(0.0, 0.0);
    int count = (int)VortexCount;

    for (int i = 0; i < count; i++)
    {
        float originX = VortexOrigins[i].x;
        float originY = VortexOrigins[i].y;
        float swirl   = VortexState[i].x;
        float radius  = VortexState[i].y;

        if (abs(swirl) < 0.0001) continue;

        float2 offset = uv - float2(originX, originY);

        float dist = length(float2(offset.x * AspectRatio, offset.y));

        if (dist > radius || dist < 0.001) continue;

        float radialFade = 1.0 - smoothstep(radius * 0.6, radius, dist);

        float swirlAngle = swirl / max(dist, 0.04) * radialFade;

        float cosA = cos(swirlAngle);
        float sinA = sin(swirlAngle);
        float2 aspectOffset = float2(offset.x * AspectRatio, offset.y);
        float2 rotated = float2(
            aspectOffset.x * cosA - aspectOffset.y * sinA,
            aspectOffset.x * sinA + aspectOffset.y * cosA
        );

        float2 rotatedUV = float2(rotated.x / AspectRatio, rotated.y) + float2(originX, originY);
        totalDisplacement += rotatedUV - uv;
    }

    float2 sampleUV = clamp(uv + totalDisplacement, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique Vortex
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
