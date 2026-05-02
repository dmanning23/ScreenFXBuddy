#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Center;      // UV-space origin of the burst
float4 LineColor;   // RGBA tint including current alpha
float  LineCount;   // number of angular segments
float  InnerRadius; // UV-radius below which pixels are transparent (expand cutoff)
float  MaxRadius;   // UV-radius above which pixels are transparent

static const float PI = 3.14159265;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv   = input.TexCoord;
    float2 dir  = uv - Center;
    float  dist = length(dir);

    // Expand cutoff: guard against atan2(0,0) singularity when InnerRadius == 0
    if (dist < max(InnerRadius, 0.0001) || dist > MaxRadius)
        return float4(0, 0, 0, 0);

    // Map angle [-π, π] → segment index [0, LineCount)
    float angle   = atan2(dir.y, dir.x);
    float segment = floor((angle / (2.0 * PI) + 0.5) * LineCount);

    // Hash the segment — roughly half of segments become lines
    float hash = frac(sin(segment * 127.1 + 311.7) * 43758.5453);
    if (hash < 0.5)
        return float4(0, 0, 0, 0);

    // Fade to zero as pixels approach the outer radius
    float edgeFade = 1.0 - smoothstep(MaxRadius * 0.7, MaxRadius, dist);

    return float4(LineColor.rgb, LineColor.a * edgeFade);
}

technique SpeedLines
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
