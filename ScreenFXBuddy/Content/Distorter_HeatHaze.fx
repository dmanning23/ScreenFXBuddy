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

float  HazeCount;
float4 HazeOrigins[8];   // .xy = (originX_uv, originY_uv)
float4 HazeState[8];     // .x = radius_uv, .y = height_uv, .z = strength (pre-faded)
float  AspectRatio;
float  HazeTime[8];

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;
    float2 totalDisplace = float2(0.0, 0.0);
    int count = (int)HazeCount;

    for (int i = 0; i < count; i++)
    {
        float originX  = HazeOrigins[i].x;
        float originY  = HazeOrigins[i].y;
        float radius   = HazeState[i].x;
        float height   = HazeState[i].y;
        float strength = HazeState[i].z;
        float time = HazeTime[i];

        float dx = uv.x - originX;
        float dy = originY - uv.y;

        if (dy < 0.0 || dy > height) continue;
        if (abs(dx) > radius) continue;

        float lateralFade = exp(-(dx * dx * AspectRatio * AspectRatio) / (radius * radius * 0.5));
        float vertFade    = 1.0 - (dy / height);

        float wave1 = sin(dy * 12.0 + time * 2.3) * sin(dy * 7.3 - time * 1.7);
        float wave2 = sin(dy *  8.1 - time * 3.1) * sin(dy * 5.7 + time * 2.1) * 0.5;

        totalDisplace.x += (wave1 + wave2) * strength * lateralFade * vertFade;
        totalDisplace.y += (wave1 * 0.15)  * strength * lateralFade * vertFade;
    }

    float2 sampleUV = clamp(uv + totalDisplace, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique HeatHaze
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
