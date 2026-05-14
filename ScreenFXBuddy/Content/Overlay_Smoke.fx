#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Origin;
float4 SmokeColor;
float  Radius;
float  Progress;
float  Time;
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
    float2 uv = input.TexCoord;

    float dx = (uv.x - Origin.x) * AspectRatio;
    float dy = uv.y - Origin.y;
    float dist = sqrt(dx * dx + dy * dy);

    float currentRadius = Radius * Progress;
    if (dist > currentRadius * 1.5) return float4(0, 0, 0, 0);

    float2 driftedUV = float2(
        uv.x + fbm(uv * 3.0 + float2(Time * 0.1, 0)) * 0.05,
        uv.y - Time * 0.04
    );

    float density = fbm((driftedUV - Origin) / (Radius + 0.001) * 2.5 + float2(Time * 0.2, 0));

    float radialFade = 1.0 - smoothstep(currentRadius * 0.5, currentRadius, dist);

    float vertBias = saturate((Origin.y - uv.y) / (currentRadius + 0.001) + 0.3);

    float lifeFade = Progress < 0.2 ? Progress / 0.2
                   : Progress < 0.7 ? 1.0
                   : 1.0 - (Progress - 0.7) / 0.3;

    float alpha = density * radialFade * vertBias * lifeFade * SmokeColor.a * 1.5;
    alpha = min(alpha, 1.0);
    return float4(SmokeColor.rgb * alpha, alpha);
}

technique Smoke
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
