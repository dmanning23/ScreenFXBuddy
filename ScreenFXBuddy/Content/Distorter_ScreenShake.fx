// ============================================================================
// Distorter_ScreenShake.fx
// Screen-shake distortion shader for ScreenFXBuddy.
//
// Applies a UV-space offset to the scene texture.  The offset (and its
// fade-over-time / direction) is computed entirely in C# by ScreenShakeLayer
// and passed in as the single Offset parameter.
// ============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// UV-space displacement to apply each frame (set by ScreenShakeLayer.Apply)
float2 Offset;

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
    float2 uv = clamp(input.TexCoord + Offset, 0.0, 1.0);
    return tex2D(SceneSampler, uv) * input.Color;
}

// ── Technique ────────────────────────────────────────────────────────────────
technique ScreenShake
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
