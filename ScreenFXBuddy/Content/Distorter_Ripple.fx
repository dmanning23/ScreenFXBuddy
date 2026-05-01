// ============================================================================
// Distorter_Ripple.fx
// Force-ripple / shockwave distortion shader for ScreenFXBuddy.
//
// Each active ripple is described as a ring:
//   RipplePositions[i].xy  — UV-space center
//   RippleRings[i].x       — outer radius (UV)
//   RippleRings[i].y       — inner radius (UV)
//   RippleRings[i].z       — strength (UV displacement at the ring crest)
//
// The C# layer (ForceRippleLayer) computes outer/inner radius each frame from
// the ripple's current age, speed, and size.  This shader does not need to
// know anything about time or wave frequency — it only draws rings.
//
// Using float4 for both arrays avoids float2/float3 packing quirks in some
// GLSL drivers; just ignore the unused components.
// ============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define MAX_RIPPLES 16

float  RippleCount;                     // passed as float to avoid int-uniform driver quirks
float4 RipplePositions[MAX_RIPPLES];    // xy = UV-space center, zw unused
float4 RippleRings[MAX_RIPPLES];        // x = outerRadius, y = innerRadius, z = strength, w unused
float  AspectRatio;                     // viewport width / height (corrects circular rings)

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

// ── Pixel shader input (matches SpriteBatch vertex output) ───────────────────
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// ── Pixel shader ─────────────────────────────────────────────────────────────
float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;
    float2 totalDisplacement = float2(0.0, 0.0);

    int count = (int)RippleCount;

    for (int i = 0; i < count; i++)
    {
        float2 center = RipplePositions[i].xy;
        float  outerR  = RippleRings[i].x;
        float  innerR  = RippleRings[i].y;
        float  strength = RippleRings[i].z;

        // Aspect-correct the delta so distance forms a circle, not an ellipse
        float2 delta = uv - center;
        delta.x *= AspectRatio;
        float dist = length(delta);

        if (dist >= innerR && dist <= outerR && dist > 0.001)
        {
            // Normalize position within the ring band to [0,1]
            float t = (dist - innerR) / (outerR - innerR);
            // sin(t*pi) peaks at the ring centre and falls smoothly to zero at both edges
            float wave = sin(t * 3.14159265);

            // Radial displacement direction, un-corrected back to UV space
            float2 dir = normalize(delta);
            dir.x /= AspectRatio;

            totalDisplacement += dir * wave * strength;
        }
    }

    float2 refractedUV = clamp(uv + totalDisplacement, 0.0, 1.0);
    return tex2D(SceneSampler, refractedUV) * input.Color;
}

// ── Technique ────────────────────────────────────────────────────────────────
technique ForceRipple
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
