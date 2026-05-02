// ============================================================================
// Distorter_GravityWave.fx
// Gravity-wave / earthquake crescent distortion shader for ScreenFXBuddy.
//
// Each active wave spawns two crescent-shaped distortion bands that travel
// left and right from the impact point.  Displacement follows the crescent
// surface normal (outward + upward along the arc), giving an organic
// pressure-wave feel.
//
// Per-instance data packed into float4 arrays (avoids float2/3 packing quirks):
//   WaveOrigins[i].xy  — UV-space impact point
//   WaveState[i].x     — travelX: how far each crescent has traveled (UV)
//   WaveState[i].y     — arcH: current crescent height (UV)
//   WaveState[i].z     — strength: peak displacement magnitude (UV)
//
// BandWidth is the UV half-width of each crescent band (passed from C#).
// AspectRatio corrects the surface normal to be screen-circular.
// ============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define MAX_WAVES 8

static const float PI = 3.14159265;

// ── Parameters ───────────────────────────────────────────────────────────────
float  WaveCount;               // passed as float to avoid int-uniform driver quirks
float4 WaveOrigins[MAX_WAVES];  // xy = UV-space origin, zw unused
float4 WaveState[MAX_WAVES];    // x = travelX, y = arcH, z = strength, w unused
float  AspectRatio;             // viewport width / height
float  BandWidth;               // UV half-width of distortion band

// ── Scene texture (bound by SpriteBatch) ─────────────────────────────────────
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

// ── Vertex shader output (matches SpriteBatch vertex output) ─────────────────
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// ── Pixel shader ─────────────────────────────────────────────────────────────
float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv                = input.TexCoord;
    float2 totalDisplacement = float2(0.0, 0.0);

    int count = (int)WaveCount;

    for (int i = 0; i < count; i++)
    {
        float originX  = WaveOrigins[i].x;
        float originY  = WaveOrigins[i].y;
        float travelX  = WaveState[i].x;
        float arcH     = WaveState[i].y;
        float strength = WaveState[i].z;

        if (arcH < 0.0001 || strength < 0.0001) continue;

        // Left crescent (s=0, side=-1) and right crescent (s=1, side=+1)
        for (int s = 0; s < 2; s++)
        {
            float side  = (s == 0) ? -1.0 : 1.0;
            float waveX = originX + side * travelX;

            float dx = uv.x - waveX;    // signed horizontal dist from wave front
            float dy = originY - uv.y;  // height above ground (positive = above)

            if (abs(dx) > BandWidth) continue;
            if (dy < 0.0 || dy > arcH) continue;

            // Gaussian falloff across band, sine curve up the crescent height
            float hFade = exp(-(dx * dx) / (BandWidth * BandWidth * 0.3));
            float vFade = sin((dy / arcH) * PI);

            // Surface normal in screen space: outward (aspect-corrected) + upward
            float nx  = side * AspectRatio;
            float ny  = -(dy / arcH);
            float len = sqrt(nx * nx + ny * ny);
            if (len < 0.0001) continue;
            nx /= len;
            ny /= len;

            // Convert back to UV space and accumulate
            totalDisplacement.x += (nx / AspectRatio) * strength * hFade * vFade;
            totalDisplacement.y +=  ny                * strength * hFade * vFade;
        }
    }

    float2 refractedUV = clamp(uv + totalDisplacement, 0.0, 1.0);
    return tex2D(SceneSampler, refractedUV) * input.Color;
}

// ── Technique ────────────────────────────────────────────────────────────────
technique GravityWave
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
