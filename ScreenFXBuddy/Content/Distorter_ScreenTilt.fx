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

float Angle;       // current rotation in radians; positive = clockwise
float AspectRatio; // width / height, for circular rotation

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv     = input.TexCoord;
    float2 center = float2(0.5, 0.5);
    float2 offset = uv - center;

    offset.x *= AspectRatio;

    float cosA = cos(Angle);
    float sinA = sin(Angle);
    float2 rotated = float2(
        offset.x * cosA - offset.y * sinA,
        offset.x * sinA + offset.y * cosA
    );

    rotated.x /= AspectRatio;

    float2 sampleUV = clamp(rotated + center, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique ScreenTilt
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
