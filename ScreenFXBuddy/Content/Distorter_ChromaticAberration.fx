// ============================================================================
// Distorter_ChromaticAberration.fx
// Expanding radial chromatic aberration for ScreenFXBuddy.
//
// Intended use: a character gets hit so hard their soul starts leaving their
// body.  The aberration ring grows outward from an origin point (the hit
// position on screen) while simultaneously fading out, so you see color
// fringing bloom outward and then dissolve.
//
// How the two parameters work together
// -------------------------------------
//   Distance — UV-space magnitude of the channel split.  Starts at 0 and
//              grows to its maximum over the effect duration, so the ring
//              expands outward.  Set by ChromaticAberrationLayer, which
//              computes:  currentDistance = maxDistance * (1 - Timer.Lerp)
//
//   Strength — Fade multiplier in [1..0].  Starts at full and drops to zero
//              as the timer expires.  Controls a lerp between the original
//              unmodified scene pixel and the aberrated result, so the ring
//              vanishes smoothly rather than snapping off.  Computed as:
//              currentStrength = Timer.Lerp
//
//   Origin   — UV-space position the ring radiates from, derived from a
//              screen-pixel position passed to ChromaticAberrationLayer
//              and divided by the viewport dimensions in Apply().
//
// Channel split
// -------------
//   Per pixel, we compute a radial direction vector from Origin to the
//   current UV.  We then sample:
//     R  — at uv + dir * Distance   (pushed outward / away from Origin)
//     G  — at uv                    (unmodified; the stable reference)
//     B  — at uv - dir * Distance   (pushed inward / toward Origin)
//   Pixels farther from Origin get a larger absolute offset naturally,
//   because dir is longer there.  The AddressU/V = Clamp on the sampler
//   keeps out-of-bounds UVs from wrapping or going black — they just hold
//   the edge colour.
// ============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// UV-space channel-split magnitude; grows from 0 → maxDistance over time
float Distance;

// Fade multiplier; decreases from 1 → 0 over time
float Strength;

// UV-space origin the ring radiates from (pixel position ÷ viewport size)
float2 Origin;

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

    // Radial direction from the origin point to this pixel.
    // Magnitude naturally increases with distance from Origin, so pixels
    // at the screen edges get more separation than those near the origin.
    float2 dir = uv - Origin;

    float r = tex2D(SceneSampler, uv + dir * Distance).r;  // pushed outward
    float g = tex2D(SceneSampler, uv).g;                   // unmodified
    float b = tex2D(SceneSampler, uv - dir * Distance).b;  // pushed inward

    float4 original  = tex2D(SceneSampler, uv);
    float4 aberrated = float4(r, g, b, 1.0);

    // Lerp back to the original scene as Strength falls to zero, so the
    // aberration fades out without leaving a residual colour shift.
    return lerp(original, aberrated, Strength) * input.Color;
}

// ── Technique ────────────────────────────────────────────────────────────────
technique ChromaticAberration
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
