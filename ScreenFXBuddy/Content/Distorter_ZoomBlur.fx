#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define MAX_INSTANCES 8

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

float InstanceCount;
float4 Origins[MAX_INSTANCES];  // xy = UV-space origin, zw unused
float4 States[MAX_INSTANCES];    // x = Strength, y = Radius, zw unused
//float  Strength;    // pre-faded by C# (peakStrength * sin(t * pi))
//float  Radius;      // UV-space outer edge of the effect
float  AspectRatio; // width / height, for circular falloff region

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

    int count = (int)InstanceCount;
    for (int i = 0; i < count; i++)
    {
        float originX  = Origins[i].x;
        float originY  = Origins[i].y;
        float strength  = States[i].x;
        float radius     = States[i].y;

        float2 offset = float2(uv.x - originX, uv.y - originY);

        float dist = length(float2(offset.x * AspectRatio, offset.y));

        if (dist > radius || dist < 0.0001)
            continue;

        float radialFade = 1.0 - smoothstep(radius * 0.5, radius, dist);

        float2 dir          = offset / dist;

        float2 displacement = dir * dist * strength * radialFade;
        totalDisplacement.x +=  displacement.x;
        totalDisplacement.y +=  displacement.y;
    }

    float2 sampleUV     = clamp(uv + totalDisplacement, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique ZoomBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
