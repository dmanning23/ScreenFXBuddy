// ============================================================================
// Overlay_SpeedLines.fx
// Speed-lines burst overlay shader for ScreenFXBuddy.
//
// Renders a radial burst of speed lines emanating from a configurable center
// point.  The screen is divided into angular segments; roughly half are lit
// using a deterministic hash, producing the classic manga speed-line look.
// An edge fade dissolves lines as they approach the outer radius.
//
// Parameters
// ----------
//   Center      — UV-space origin of the burst (typically screen center).
//
//   LineColor   — RGBA tint applied to all lit segments.  The C# layer
//                 modulates the alpha over the effect lifetime.
//
//   LineCount   — Number of angular segments.  Passed as float to avoid
//                 int-uniform driver quirks on some GLES targets.
//
//   InnerRadius — UV-radius below which pixels are transparent.  Expanding
//                 this value creates the "clear core" of the burst.
//
//   MaxRadius   — UV-radius above which pixels are transparent.  Defines the
//                 outer edge of the speed-line ring.
//
//   AspectRatio — Viewport width / height.  Used to correct dir.x so that
//                 radii and angular segments are uniform in screen space
//                 rather than stretched on non-square viewports.  Set by
//                 SpeedLinesLayer each frame.
//
//   Seed        — Per-instance random float generated in SpeedLinesInstance
//                 constructor.  Offsets the angular hash so each triggered
//                 burst has a unique line layout.
// ============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// ── Parameters ───────────────────────────────────────────────────────────────
float2 Center;      // UV-space origin of the burst
float4 LineColor;   // RGBA tint including current alpha
float  LineCount;   // passed as float to avoid int-uniform driver quirks
float  InnerRadius; // UV-radius below which pixels are transparent (expand cutoff)
float  MaxRadius;   // UV-radius above which pixels are transparent
float  AspectRatio; // viewport width / height, set by SpeedLinesLayer
float  Seed;        // per-instance random offset, varies line pattern each trigger

static const float PI = 3.14159265;

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
    float2 uv   = input.TexCoord;
    float2 dir  = uv - Center;

    // Guard: avoid divide-by-zero if MaxRadius is not yet set
    if (MaxRadius < 0.0001)
        return float4(0, 0, 0, 0);

    // Aspect-ratio correction so radii and angular segments are circular in
    // screen space rather than elliptical on non-square viewports
    float2 dirCorrected = float2(dir.x * AspectRatio, dir.y);
    float  dist = length(dirCorrected);

    // Expand cutoff: guard against atan2(0,0) singularity when InnerRadius == 0
    if (dist < max(InnerRadius, 0.0001) || dist > MaxRadius)
        return float4(0, 0, 0, 0);

    // Map angle [-π, π] → segment index [0, LineCount)
    // fmod guards against segment == LineCount at the exact +π boundary
    float angle   = atan2(dirCorrected.y, dirCorrected.x);
    float segment = fmod(floor((angle / (2.0 * PI) + 0.5) * LineCount), LineCount);

    // Hash the segment — roughly half of segments become lines.
    // Seed offsets the hash so each triggered instance has a unique pattern.
    float hash = frac(sin(segment * 127.1 + 311.7 + Seed) * 43758.5453);
    if (hash < 0.5)
        return float4(0, 0, 0, 0);

    // Fade to zero as pixels approach the outer radius
    float edgeFade = 1.0 - smoothstep(MaxRadius * 0.7, MaxRadius, dist);

    return float4(LineColor.rgb, LineColor.a * edgeFade);
}

// ── Technique ────────────────────────────────────────────────────────────────
technique SpeedLines
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
