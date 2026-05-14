#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Origin;
float4 TintColor;
float  Radius;
float  Progress;
float  AspectRatio;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = frac(sin(dot(i,               float2(127.1, 311.7))) * 43758.5453);
    float b = frac(sin(dot(i + float2(1,0), float2(127.1, 311.7))) * 43758.5453);
    float c = frac(sin(dot(i + float2(0,1), float2(127.1, 311.7))) * 43758.5453);
    float d = frac(sin(dot(i + float2(1,1), float2(127.1, 311.7))) * 43758.5453);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p)
{
    float v = 0.0, amp = 0.5;
    for (int i = 0; i < 5; i++) { v += amp * valueNoise(p); p *= 2.0; amp *= 0.5; }
    return v;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv  = input.TexCoord;
    float2 off = float2((uv.x - Origin.x) * AspectRatio, uv.y - Origin.y);
    float  dist = length(off);

    if (dist < 0.0001) return float4(0, 0, 0, 0);

    float frostRadius = Radius * min(Progress * 2.0, 1.0);

    if (dist > frostRadius * 1.15) return float4(0, 0, 0, 0);

    float angle = atan2(off.y, off.x);
    float2 crystalCoord = float2(
        angle * 18.0 / 3.14159,
        dist  *  6.0
    );

    float density = fbm(crystalCoord);

    float crystals = smoothstep(0.38, 0.62, density);

    float radialFade = 1.0 - smoothstep(frostRadius * 0.5, frostRadius, dist);

    float lifeFade = Progress < 0.6 ? 1.0 : 1.0 - (Progress - 0.6) / 0.4;

    float sparkle = smoothstep(0.72, 0.85, density) * 0.5;

    float alpha = (crystals * 0.6 + sparkle) * radialFade * lifeFade * TintColor.a;
    return float4(TintColor.rgb * alpha, alpha);
}

technique Frost
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
