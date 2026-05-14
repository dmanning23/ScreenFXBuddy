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

float2 Origin;
float  Strength;
float  NumCells;
float  Seed;
float  Shatter;
float  AspectRatio;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float2 site(float index)
{
    float2 p;
    p.x = frac(sin(index * 127.1 + Seed)           * 43758.5453);
    p.y = frac(sin(index * 311.7 + Seed + 100.0)   * 43758.5453);
    return p;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;

    float minDist1    = 1e9;
    float minDist2    = 1e9;
    float nearestIdx  = 0;
    float2 nearestPos = float2(0, 0);

    int count = (int)NumCells;
    for (int k = 0; k < count; k++)
    {
        float2 s = site((float)k);
        float d = length(float2((uv.x - s.x) * AspectRatio, uv.y - s.y));
        if (d < minDist1)
        {
            minDist2   = minDist1;
            minDist1   = d;
            nearestIdx = (float)k;
            nearestPos = s;
        }
        else if (d < minDist2)
        {
            minDist2 = d;
        }
    }

    float2 cellToImpact = nearestPos - Origin;
    if (length(cellToImpact) < 0.001) cellToImpact = float2(1, 0);
    float2 dispDir = normalize(cellToImpact);

    float impactDist = length(float2((nearestPos.x - Origin.x) * AspectRatio, nearestPos.y - Origin.y));
    float falloff    = 1.0 / (1.0 + impactDist * 4.0);

    float2 displacement = dispDir * Strength * Shatter * falloff;

    float2 sampleUV  = clamp(uv + displacement, 0.0, 1.0);
    float4 sceneColor = tex2D(SceneSampler, sampleUV);

    float crackWidth = 0.006;
    float boundary   = minDist2 - minDist1;
    float crackAlpha = Shatter * (1.0 - smoothstep(0, crackWidth, boundary));
    float4 crackColor = float4(0.85, 0.95, 1.0, crackAlpha);

    return lerp(sceneColor, crackColor, crackAlpha) * input.Color;
}

technique GlassShatter
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
