# Gravity Wave Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the `GravityWaveLayer` stub with a working crescent distortion effect — two expanding arcs of pixel displacement that shoot left and right from an impact point, like an earthquake shockwave.

**Architecture:** `GravityWaveLayer` implements `IDistortionLayer`, following the `ForceRippleLayer` pattern exactly: per-instance state in a `List<WaveInstance>`, packed into `float4[8]` arrays each frame, one shader draw call per `Apply()`. The HLSL shader generates two crescent-shaped displacement bands (left + right) per instance, pushing pixels along the crescent's surface normal (outward + upward along the arc).

**Tech Stack:** MonoGame/DesktopGL, HLSL (ps_3_0 / ps_4_0_level_9_1), `MathHelper.Lerp` for arc height interpolation. Build requires `MGFXC_WINE_PATH=/Users/danmanning/.winemonogame` set in the shell.

---

## File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Create | `ScreenFXBuddy/Content/Distorter_GravityWave.fx` | Crescent distortion shader |
| Modify | `ScreenFXBuddy/Effects/GravityWaveLayer.cs` | Replace stub — full implementation |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register the new shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Update `TriggerGravityWave` signature |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Replace D2 binding, add I/O variants |

---

### Task 1: Shader — Distorter_GravityWave.fx

**Files:**
- Create: `ScreenFXBuddy/Content/Distorter_GravityWave.fx`

Reference shader for structure: `ScreenFXBuddy/Content/Distorter_Ripple.fx`

- [ ] **Step 1: Create the shader file**

```hlsl
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
```

- [ ] **Step 2: Commit**

```bash
git add ScreenFXBuddy/Content/Distorter_GravityWave.fx
git commit -m "feat: add Distorter_GravityWave.fx crescent distortion shader"
```

---

### Task 2: GravityWaveLayer — replace stub

**Files:**
- Modify: `ScreenFXBuddy/Effects/GravityWaveLayer.cs` (full rewrite)

Read the existing stub first. The file currently uses `Debug_Color` as a placeholder and has a `record struct WaveInstance(Vector2 Position, float Strength, float Age)` that must be replaced.

Reference implementation to follow: `ScreenFXBuddy/Effects/ForceRippleLayer.cs`

- [ ] **Step 1: Replace `GravityWaveLayer.cs` entirely**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class GravityWaveLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pWaveCount;
    private EffectParameter _pWaveOrigins;
    private EffectParameter _pWaveState;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pBandWidth;

    private readonly List<WaveInstance> _instances = new();

    private const int   MaxInstances = 8;
    private const float BandWidth    = 0.06f;

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];

    public bool IsActive => _instances.Count > 0;

    public GravityWaveLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Distorter_GravityWave");
        _pWaveCount   = _effect.Parameters["WaveCount"];
        _pWaveOrigins = _effect.Parameters["WaveOrigins"];
        _pWaveState   = _effect.Parameters["WaveState"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pBandWidth   = _effect.Parameters["BandWidth"];
    }

    public void Trigger(
        Vector2 position,
        float strength    = 0.04f,
        float startHeight = 0.05f,
        float endHeight   = 0.25f,
        float speed       = 0.5f,
        float duration    = 1.5f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new WaveInstance(position, strength, startHeight, endHeight, speed, duration, 0f));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Age = inst.Age + dt };
            if (inst.Age >= inst.Duration)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        int count = Math.Min(_instances.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var inst = _instances[i];
            float t = inst.Age / inst.Duration;

            _originBuffer[i] = new Vector4(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height,
                0f, 0f);

            _stateBuffer[i] = new Vector4(
                inst.Age * inst.Speed,
                MathHelper.Lerp(inst.StartHeight, inst.EndHeight, t),
                inst.Strength * (1f - t),
                0f);
        }

        _pWaveCount.SetValue((float)count);
        _pWaveOrigins.SetValue(_originBuffer);
        _pWaveState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);
        _pBandWidth.SetValue(BandWidth);

        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }

    private record struct WaveInstance(
        Vector2 Position,
        float Strength,
        float StartHeight,
        float EndHeight,
        float Speed,
        float Duration,
        float Age);
}
```

- [ ] **Step 2: Build the library to confirm it compiles**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/GravityWaveLayer.cs
git commit -m "feat: implement GravityWaveLayer crescent distortion effect"
```

---

### Task 3: Register shader in Content Pipeline

**Files:**
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

- [ ] **Step 1: Read `Content.mgcb` to find the insertion point**

The file is at `ScreenFXBuddy.Example/Content/Content.mgcb`. Add the new entry after the `Distorter_ScreenShake.fx` block and before the `Braid_screenshot8.jpg` block:

```
#begin ../../ScreenFXBuddy/Content/Distorter_GravityWave.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_GravityWave.fx;Distorter_GravityWave.fx
```

- [ ] **Step 2: Build the Example project — this compiles the shader**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example/ScreenFXBuddy.Example.csproj
```

Expected: `Build succeeded` with 0 errors. The shader compiles to `Distorter_GravityWave.xnb` in the content output directory.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: register Distorter_GravityWave.fx in content pipeline"
```

---

### Task 4: Update TriggerGravityWave in ScreenFXComponent

**Files:**
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`

The existing `TriggerGravityWave` method takes `(Vector2 position, float strength = 1f)`. Replace it with the new signature that matches `GravityWaveLayer.Trigger`.

- [ ] **Step 1: Find and replace `TriggerGravityWave` in `ScreenFXComponent.cs`**

Find the existing method (it will look like):
```csharp
public void TriggerGravityWave(Vector2 position, float strength = 1f)
    => GravityWave.Trigger(position, strength);
```

Replace with:
```csharp
public void TriggerGravityWave(
    Vector2 position,
    float strength    = 0.04f,
    float startHeight = 0.05f,
    float endHeight   = 0.25f,
    float speed       = 0.5f,
    float duration    = 1.5f)
    => GravityWave.Trigger(position, strength, startHeight, endHeight, speed, duration);
```

- [ ] **Step 2: Build the library**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/ScreenFXComponent.cs
git commit -m "feat: update TriggerGravityWave signature with crescent parameters"
```

---

### Task 5: Update example keybindings and visual test

**Files:**
- Modify: `ScreenFXBuddy.Example/Game1.cs`

- [ ] **Step 1: Read `Game1.cs` to find the existing D2 binding**

The current D2 binding looks like:
```csharp
if (keys.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2))
    _screenFX.TriggerGravityWave(center);
```

Replace that single line with three bindings. Use `ScreenHeight * 0.75f` as the ground Y position (lower quarter of screen, a typical ground level):

```csharp
if (keys.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2))
    _screenFX.TriggerGravityWave(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f));

// Slow wide wave — big expanding crescent
if (keys.IsKeyDown(Keys.I) && !_prevKeys.IsKeyDown(Keys.I))
    _screenFX.TriggerGravityWave(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.06f, startHeight: 0.02f, endHeight: 0.4f, speed: 0.3f, duration: 2.5f);

// Fast tight wave — snappy ground skim
if (keys.IsKeyDown(Keys.O) && !_prevKeys.IsKeyDown(Keys.O))
    _screenFX.TriggerGravityWave(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.03f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.9f, duration: 0.8f);
```

- [ ] **Step 2: Build the full solution**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Run the example and visually verify**

```bash
dotnet run --project ScreenFXBuddy.Example/ScreenFXBuddy.Example.csproj
```

Verify:
- **2** — Two crescent waves shoot left and right from the center of the lower-third of the screen, expanding vertically as they travel, then fading. Scene pixels visibly distort as the wave front passes.
- **I** — Wide slow wave, crescent grows tall as it travels.
- **O** — Tight fast wave, stays low to the ground.
- Press **2** several times quickly — up to 8 simultaneous instances stack correctly.
- No green tint (the old `Debug_Color` stub is fully replaced).

- [ ] **Step 4: Commit**

```bash
git add ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: update gravity wave example keybindings (2, I, O)"
```
