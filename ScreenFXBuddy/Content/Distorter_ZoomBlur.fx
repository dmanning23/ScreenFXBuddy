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

float2 Origin;      // UV-space origin of the blur
float  Strength;    // pre-faded by C# (peakStrength * sin(t * pi))
float  Radius;      // UV-space outer edge of the effect
float  AspectRatio; // width / height, for circular falloff region

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv     = input.TexCoord;
    float2 offset = uv - Origin;

    float dist = length(float2(offset.x * AspectRatio, offset.y));

    if (dist > Radius || dist < 0.0001)
        return tex2D(SceneSampler, uv) * input.Color;

    float radialFade = 1.0 - smoothstep(Radius * 0.5, Radius, dist);

    float2 dir          = offset / dist;
    float2 displacement = dir * dist * Strength * radialFade;
    float2 sampleUV     = clamp(uv + displacement, 0.0, 1.0);

    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique ZoomBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
