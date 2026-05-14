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

float4 TintColor;    // RGBA — e.g. (0.39, 0.63, 1.0, 1.0) for icy blue
float  Intensity;    // 0→1: how strongly the effect is applied
float  AspectRatio;  // width / height, for circular vignette

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;

    float2 offset = float2((uv.x - 0.5) * AspectRatio, uv.y - 0.5);
    float  dist   = length(offset);

    float4 original = tex2D(SceneSampler, uv);

    float  luma = dot(original.rgb, float3(0.299, 0.587, 0.114));
    float3 gray = float3(luma, luma, luma);

    float3 tinted = lerp(gray, gray * TintColor.rgb * 2.0, 0.5);

    float vignette  = smoothstep(0.35, 0.75, dist) * Intensity * 0.7;
    float3 vignetted = tinted * (1.0 - vignette);

    float3 result = lerp(original.rgb, vignetted, Intensity);

    return float4(result, original.a) * input.Color;
}

technique FreezeFrame
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
