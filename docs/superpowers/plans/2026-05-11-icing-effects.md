# Icing Effects Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete three "icing" visual effects — HeatHazeLayer (replace stub with localized heat shimmer column), SmokeLayer (new FBM procedural smoke overlay), and GlassShatterLayer (new procedural Voronoi crack distortion).

**Architecture:** HeatHazeLayer replaces the existing `Debug_Color` stub with a real shader, following `GravityWaveLayer`'s packed-array pattern (up to 8 instances). SmokeLayer is a new `IOverlayLayer` following `SpeedLinesLayer`'s one-draw-call-per-instance pattern (up to 4). GlassShatterLayer is a new single-instance `IDistortionLayer` using a procedural Voronoi shader driven by a per-trigger random seed.

**Tech Stack:** MonoGame, HLSL (all three), FBM value noise (Smoke), procedural Voronoi (GlassShatter), packed shader arrays (HeatHaze), C# 12.

**Key binding note:** The specs for this group assign H/J/K (HeatHaze), N/M/OemComma (Smoke), and G/[/] (GlassShatter) — all of which are already bound in `Game1.cs` to ForceRipple, HitFlash/SpeedLines, and ForceRipple/ChromaticAberration respectively. This plan uses free keys instead: **Tab/Back/Space** for HeatHaze, **OemTilde/OemQuotes/OemPipe** for Smoke, **OemQuestion/NumPad0/NumPadDecimal** for GlassShatter. D5 is updated from the old 2-parameter HeatHaze call to the new 5-parameter signature.

---

## Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `ScreenFXBuddy/Effects/HeatHazeLayer.cs` | Replace Debug_Color stub with full implementation |
| Create | `ScreenFXBuddy/Content/Distorter_HeatHaze.fx` | Layered-sine heat shimmer column shader |
| Create | `ScreenFXBuddy/Effects/SmokeLayer.cs` | 4 instances, one draw call per instance |
| Create | `ScreenFXBuddy/Content/Overlay_Smoke.fx` | FBM procedural upward-drifting cloud |
| Create | `ScreenFXBuddy/Effects/GlassShatterLayer.cs` | Single instance, sin(t·π) push-and-return |
| Create | `ScreenFXBuddy/Content/Distorter_GlassShatter.fx` | Procedural Voronoi distortion + crack lines |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Update TriggerHeatHaze signature; add Smoke, GlassShatter |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register all three shaders |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Update D5 binding; add new bindings |

---

### Task 1: Complete HeatHazeLayer

**Spec:** `docs/superpowers/specs/2026-05-11-heat-haze-design.md`

**Files:**
- Modify: `ScreenFXBuddy/Effects/HeatHazeLayer.cs`
- Create: `ScreenFXBuddy/Content/Distorter_HeatHaze.fx`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: The stub loads `Debug_Color` and ignores all shader parameters. The replacement follows `GravityWaveLayer`'s architecture: up to 8 instances packed into two `float4[8]` arrays (`_originBuffer` and `_stateBuffer`) uploaded with one `SetValue` call each, then a single `spriteBatch.Begin/Draw/End`. The shader displaces pixels horizontally (and slightly vertically) using two layered sine waves that animate with `Time`. Only pixels in the rising column above the source are affected; a Gaussian lateral falloff keeps distortion inside the column width.

- [ ] **Step 1: Create Distorter_HeatHaze.fx**

```hlsl
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

float  HazeCount;        // number of active instances (as float)
float4 HazeOrigins[8];   // .xy = (originX_uv, originY_uv)
float4 HazeState[8];     // .x = radius_uv, .y = height_uv, .z = strength (pre-faded)
float  AspectRatio;
float  Time;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;
    float2 totalDisplace = float2(0.0, 0.0);
    int count = (int)HazeCount;

    for (int i = 0; i < count; i++)
    {
        float originX  = HazeOrigins[i].x;
        float originY  = HazeOrigins[i].y;
        float radius   = HazeState[i].x;
        float height   = HazeState[i].y;
        float strength = HazeState[i].z;

        float dx = uv.x - originX;     // signed horizontal offset from column center
        float dy = originY - uv.y;     // height above source (positive = above)

        // Only affect pixels in the rising column above the source
        if (dy < 0.0 || dy > height) continue;
        if (abs(dx) > radius) continue;

        // Gaussian lateral falloff from column centerline
        // Aspect-corrected so the column is a true column, not an ellipse on wide screens
        float lateralFade = exp(-(dx * dx * AspectRatio * AspectRatio) / (radius * radius * 0.5));

        // Linear vertical falloff: full at source, zero at top of column
        float vertFade = 1.0 - (dy / height);

        // Two layered sine waves at different frequencies and speeds
        // Product of two sines creates a non-repeating organic shimmer
        float wave1 = sin(dy * 12.0 + Time * 2.3) * sin(dy * 7.3 - Time * 1.7);
        float wave2 = sin(dy *  8.1 - Time * 3.1) * sin(dy * 5.7 + Time * 2.1) * 0.5;

        totalDisplace.x += (wave1 + wave2) * strength * lateralFade * vertFade;
        totalDisplace.y += (wave1 * 0.15)  * strength * lateralFade * vertFade;
    }

    float2 sampleUV = clamp(uv + totalDisplace, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique HeatHaze
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Replace HeatHazeLayer.cs with the full implementation**

Replace the entire file:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class HeatHazeLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pHazeCount;
    private EffectParameter _pHazeOrigins;
    private EffectParameter _pHazeState;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pTime;

    private record struct HazeInstance(
        Vector2 Position,  // pixel-space heat source
        float Strength,    // peak displacement magnitude in UV units
        float Radius,      // horizontal spread (UV units) — half-width of heat column
        float Height,      // how high distortion rises above source (UV units)
        float Duration,
        float Age);

    private const int MaxInstances = 8;
    private readonly List<HazeInstance> _instances = new();

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];

    private float _time;

    public bool IsActive => _instances.Count > 0;

    public HeatHazeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Distorter_HeatHaze");
        _pHazeCount   = _effect.Parameters["HazeCount"];
        _pHazeOrigins = _effect.Parameters["HazeOrigins"];
        _pHazeState   = _effect.Parameters["HazeState"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pTime        = _effect.Parameters["Time"];
    }

    /// <param name="position">Pixel-space heat source position.</param>
    /// <param name="strength">Peak horizontal displacement in UV units. 0.02 is subtle; 0.05 is dramatic.</param>
    /// <param name="radius">Column half-width in UV units. 0.15 = roughly 15% of screen width.</param>
    /// <param name="height">How high the haze rises above the source, in UV units. 0.40 = 40% of screen height.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.02f, float radius = 0.15f,
        float height = 0.40f, float duration = 3.0f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new HazeInstance(position, strength, radius, height, duration, 0f));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;

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
            float fadedStrength = inst.Strength * (1f - t);

            _originBuffer[i] = new Vector4(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height,
                0f, 0f);

            _stateBuffer[i] = new Vector4(inst.Radius, inst.Height, fadedStrength, 0f);
        }

        _pHazeCount.SetValue((float)count);
        _pHazeOrigins.SetValue(_originBuffer);
        _pHazeState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);
        _pTime.SetValue(_time);

        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
```

- [ ] **Step 3: Register shader in Content.mgcb**

Add after the `Distorter_Vortex.fx` block in `ScreenFXBuddy.Example/Content/Content.mgcb`:

```
#begin ../../ScreenFXBuddy/Content/Distorter_HeatHaze.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_HeatHaze.fx;Distorter_HeatHaze.fx
```

- [ ] **Step 4: Update TriggerHeatHaze in ScreenFXComponent.cs**

Find the existing `TriggerHeatHaze` method:

```csharp
public void TriggerHeatHaze(float intensity, float duration)
    => HeatHaze.Trigger(intensity, duration);
```

Replace with the new signature:

```csharp
public void TriggerHeatHaze(Vector2 position, float strength = 0.02f, float radius = 0.15f,
    float height = 0.40f, float duration = 3.0f)
    => HeatHaze.Trigger(position, strength, radius, height, duration);
```

- [ ] **Step 5: Update Game1.cs**

Find and replace the existing D5 HeatHaze binding:

```csharp
if (keys.IsKeyDown(Keys.D5) && !_prevKeys.IsKeyDown(Keys.D5))
    _screenFX.TriggerHeatHaze(1f, 2f);
```

Replace with updated D5 and add Tab/Back/Space variants:

```csharp
// D5: default heat haze — gentle shimmer rising from lower center
if (keys.IsKeyDown(Keys.D5) && !_prevKeys.IsKeyDown(Keys.D5))
    _screenFX.TriggerHeatHaze(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f));

// Tab: intense wide haze — large explosion aftermath
if (keys.IsKeyDown(Keys.Tab) && !_prevKeys.IsKeyDown(Keys.Tab))
    _screenFX.TriggerHeatHaze(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.05f, radius: 0.3f, height: 0.7f, duration: 4.0f);

// Back: tight strong burst — engine exhaust / small fire
if (keys.IsKeyDown(Keys.Back) && !_prevKeys.IsKeyDown(Keys.Back))
    _screenFX.TriggerHeatHaze(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.04f, radius: 0.06f, height: 0.25f, duration: 2.0f);
```

- [ ] **Step 6: Verify and commit**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

Run and test:
- **D5**: A shimmering column rises from the lower-center of the screen for ~3 seconds
- **Tab**: Wide, tall, intense shimmer column, longer duration
- **Back**: Tight narrow column, strong displacement

```bash
git add ScreenFXBuddy/Content/Distorter_HeatHaze.fx \
        ScreenFXBuddy/Effects/HeatHazeLayer.cs \
        ScreenFXBuddy/ScreenFXComponent.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb \
        ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: complete HeatHazeLayer — layered-sine column shimmer, 8 instances"
```

---

### Task 2: Create SmokeLayer

**Spec:** `docs/superpowers/specs/2026-05-11-smoke-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Overlay_Smoke.fx`
- Create: `ScreenFXBuddy/Effects/SmokeLayer.cs`
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`
- Modify: `ScreenFXBuddy.Example/Game1.cs`

Context: FBM is evaluated per pixel to generate a procedural cloud density. The sample coordinate drifts upward over time (`y -= Time * 0.04`) so the cloud appears to billow upward. A turbulence pass (`fbm(uv * 3.0 + ...)`) adds lateral wobble. `VertBias` suppresses smoke appearing below the emitter. One draw call per instance (same pattern as SpeedLinesLayer) with `BlendState.Additive`.

- [ ] **Step 1: Create Overlay_Smoke.fx**

```hlsl
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Origin;      // UV-space emitter position
float4 SmokeColor;  // RGBA smoke tint
float  Radius;      // max spread radius (UV units)
float  Progress;    // 0→1 over lifetime
float  Time;        // accumulated time (drives FBM drift)
float  AspectRatio; // width / height

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

    // Aspect-correct distance from emitter
    float dx   = (uv.x - Origin.x) * AspectRatio;
    float dy   = uv.y - Origin.y;
    float dist = sqrt(dx * dx + dy * dy);

    float currentRadius = Radius * Progress;
    if (dist > currentRadius * 1.5) return float4(0, 0, 0, 0);

    // Animated sample coordinate: lateral turbulence + upward drift
    float2 driftedUV = float2(
        uv.x + fbm(uv * 3.0 + float2(Time * 0.1, 0.0)) * 0.05,
        uv.y - Time * 0.04
    );

    // FBM cloud density in the drifted, origin-relative coordinate space
    float2 sampleCoord = (driftedUV - Origin) / (Radius + 0.001) * 2.5 + float2(Time * 0.2, 0.0);
    float  density     = fbm(sampleCoord);

    // Radial envelope: smooth falloff at cloud edge
    float radialFade = 1.0 - smoothstep(currentRadius * 0.5, currentRadius, dist);

    // Vertical bias: suppress below origin, favour above
    float vertBias = saturate((Origin.y - uv.y) / (currentRadius + 0.001) + 0.3);

    // Lifetime fade: ramp in over 20%, hold until 70%, ramp out over 30%
    float lifeFade = Progress < 0.2 ? Progress / 0.2
                   : Progress < 0.7 ? 1.0
                   : 1.0 - (Progress - 0.7) / 0.3;

    float alpha = density * radialFade * vertBias * lifeFade * SmokeColor.a * 0.6;
    return float4(SmokeColor.rgb * alpha, alpha);
}

technique Smoke
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create SmokeLayer.cs**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class SmokeLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;

    private EffectParameter _pOrigin;
    private EffectParameter _pSmokeColor;
    private EffectParameter _pRadius;
    private EffectParameter _pProgress;
    private EffectParameter _pTime;
    private EffectParameter _pAspectRatio;

    private record struct SmokeInstance(
        Vector2 Position,  // pixel-space emitter origin
        Vector4 Color,     // RGBA pre-converted via .ToVector4()
        float Radius,
        float Duration,
        float Age);

    private const int MaxInstances = 4;
    private readonly List<SmokeInstance> _instances = new();
    private float _time;

    public bool IsActive => _instances.Count > 0;

    public SmokeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Overlay_Smoke");
        _pOrigin      = _effect.Parameters["Origin"];
        _pSmokeColor  = _effect.Parameters["SmokeColor"];
        _pRadius      = _effect.Parameters["Radius"];
        _pProgress    = _effect.Parameters["Progress"];
        _pTime        = _effect.Parameters["Time"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="position">Pixel-space emitter origin.</param>
    /// <param name="color">Smoke color. Color.Gray = default smoke; new Color(30,30,30) = thick black smoke; Color.WhiteSmoke = steam.</param>
    /// <param name="radius">Max spread radius in UV units. 0.15 = roughly 15% of screen width.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, Color color, float radius = 0.15f, float duration = 2.0f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new SmokeInstance(position, color.ToVector4(), radius, duration, 0f));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _time += dt;

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

    public void Apply(SpriteBatch spriteBatch)
    {
        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        foreach (var inst in _instances)
        {
            float progress = MathHelper.Clamp(inst.Age / inst.Duration, 0f, 1f);
            var uvOrigin   = new Vector2(inst.Position.X / vp.Width, inst.Position.Y / vp.Height);

            _pOrigin.SetValue(uvOrigin);
            _pSmokeColor.SetValue(inst.Color);
            _pRadius.SetValue(inst.Radius);
            _pProgress.SetValue(progress);
            _pTime.SetValue(_time);
            _pAspectRatio.SetValue(aspectRatio);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                _effect);
            spriteBatch.Draw(_whitePixel, vp.Bounds, Color.White);
            spriteBatch.End();
        }
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
```

- [ ] **Step 3: Register shader in Content.mgcb**

Add after the `Distorter_HeatHaze.fx` block:

```
#begin ../../ScreenFXBuddy/Content/Overlay_Smoke.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Overlay_Smoke.fx;Overlay_Smoke.fx
```

- [ ] **Step 4: Update ScreenFXComponent.cs**

**Add property** (after `GlassShatter` — or wherever the other overlay layers are declared):

```csharp
public SmokeLayer Smoke { get; private set; }
```

**In `LoadContent`**, add:

```csharp
Smoke = new SmokeLayer(GraphicsDevice);
```

**Update `OverlayLayers.AddRange`** to include `Smoke`:

```csharp
OverlayLayers.AddRange(new IOverlayLayer[]
    { HitFlash, AnimeSuper, Letterbox, SpeedLines, Electric, Frost, Smoke });
```

**Add trigger method**:

```csharp
public void TriggerSmoke(Vector2 position, Color color, float radius = 0.15f, float duration = 2.0f)
    => Smoke.Trigger(position, color, radius, duration);
```

- [ ] **Step 5: Add Game1.cs bindings**

```csharp
// OemTilde: gray smoke — default
if (keys.IsKeyDown(Keys.OemTilde) && !_prevKeys.IsKeyDown(Keys.OemTilde))
    _screenFX.TriggerSmoke(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f), Color.Gray);

// OemQuotes: black smoke — thick explosion aftermath
if (keys.IsKeyDown(Keys.OemQuotes) && !_prevKeys.IsKeyDown(Keys.OemQuotes))
    _screenFX.TriggerSmoke(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        new Color(30, 30, 30), radius: 0.25f, duration: 3.5f);

// OemPipe: white steam — fast dissipating
if (keys.IsKeyDown(Keys.OemPipe) && !_prevKeys.IsKeyDown(Keys.OemPipe))
    _screenFX.TriggerSmoke(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        Color.WhiteSmoke, radius: 0.1f, duration: 1.0f);
```

- [ ] **Step 6: Verify and commit**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
dotnet run --project ScreenFXBuddy.Example
```

Test: `~` shows gray smoke billowing upward; `'` shows thick dark smoke; `\` shows fast white steam puff.

```bash
git add ScreenFXBuddy/Content/Overlay_Smoke.fx \
        ScreenFXBuddy/Effects/SmokeLayer.cs \
        ScreenFXBuddy/ScreenFXComponent.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb \
        ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: add SmokeLayer — FBM procedural smoke cloud, 4 instances, configurable color"
```

---

### Task 3: Create GlassShatterLayer

**Spec:** `docs/superpowers/specs/2026-05-11-glass-shatter-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Distorter_GlassShatter.fx`
- Create: `ScreenFXBuddy/Effects/GlassShatterLayer.cs`
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`
- Modify: `ScreenFXBuddy.Example/Game1.cs`

Context: The shader iterates over `NumCells` Voronoi sites per pixel, finding the nearest and second-nearest. Displacement pushes each pixel's cell away from the impact origin, scaled by proximity to origin (close cells move more). Crack lines appear where `minDist2 - minDist1` is small (near cell boundaries). The `Shatter` parameter (`sin(t * π)`, computed in C#) drives both displacement magnitude and crack line alpha — everything starts and ends at zero. The random seed is generated at trigger time so each shatter has a different crack layout.

- [ ] **Step 1: Create Distorter_GlassShatter.fx**

```hlsl
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

float2 Origin;    // UV-space impact point
float  Strength;  // peak displacement magnitude (UV units)
float  NumCells;  // number of Voronoi sites
float  Seed;      // per-trigger random float (0–1000)
float  Shatter;   // animation value 0→1→0 (sin curve, computed in C#)
float  AspectRatio;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// Generate a Voronoi site position from its index and the global Seed
float2 site(float index)
{
    float2 p;
    p.x = frac(sin(index * 127.1 + Seed)         * 43758.5453);
    p.y = frac(sin(index * 311.7 + Seed + 100.0) * 43758.5453);
    return p;
}

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;

    // Find nearest and second-nearest Voronoi sites
    float minDist1   = 1e9;
    float minDist2   = 1e9;
    float2 nearestPos = float2(0.5, 0.5);

    int count = (int)NumCells;
    for (int k = 0; k < count; k++)
    {
        float2 s = site((float)k);
        float  d = length(float2((uv.x - s.x) * AspectRatio, uv.y - s.y));
        if (d < minDist1)
        {
            minDist2    = minDist1;
            minDist1    = d;
            nearestPos  = s;
        }
        else if (d < minDist2)
        {
            minDist2 = d;
        }
    }

    // Direction: push this cell's pixels away from impact origin
    float2 cellToOrigin = nearestPos - Origin;
    if (length(cellToOrigin) < 0.001) cellToOrigin = float2(1.0, 0.0);
    float2 dispDir = normalize(cellToOrigin);

    // Radial falloff: cells near impact displace most
    float impactDist = length(float2((nearestPos.x - Origin.x) * AspectRatio, nearestPos.y - Origin.y));
    float falloff    = 1.0 / (1.0 + impactDist * 4.0);

    float2 displacement = dispDir * Strength * Shatter * falloff;
    float2 sampleUV     = clamp(uv + displacement, 0.0, 1.0);
    float4 sceneColor   = tex2D(SceneSampler, sampleUV);

    // Crack lines: bright where cell boundaries are close
    float crackWidth = 0.006;
    float boundary   = minDist2 - minDist1;
    float crackAlpha = Shatter * (1.0 - smoothstep(0.0, crackWidth, boundary));
    float4 crackColor = float4(0.85, 0.95, 1.0, crackAlpha);

    // Composite: distorted scene with crack overlay
    return lerp(sceneColor, crackColor, crackAlpha) * input.Color;
}

technique GlassShatter
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create GlassShatterLayer.cs**

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class GlassShatterLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pOrigin;
    private EffectParameter _pStrength;
    private EffectParameter _pNumCells;
    private EffectParameter _pSeed;
    private EffectParameter _pShatter;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private record struct ShatterInstance(
        Vector2 Position,
        float Strength,
        int NumCells,
        float Seed,
        float Duration,
        float Age);

    private ShatterInstance? _instance;

    public bool IsActive => _instance.HasValue;

    public GlassShatterLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_GlassShatter");
        _pOrigin       = _effect.Parameters["Origin"];
        _pStrength     = _effect.Parameters["Strength"];
        _pNumCells     = _effect.Parameters["NumCells"];
        _pSeed         = _effect.Parameters["Seed"];
        _pShatter      = _effect.Parameters["Shatter"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="position">Pixel-space impact point. The cracks radiate from here.</param>
    /// <param name="strength">Peak displacement magnitude in UV units. 0.04 = subtle; 0.07 = dramatic.</param>
    /// <param name="numCells">Number of Voronoi shards. 8 = few large shards; 20 = default; 35 = many fine shards.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.04f, int numCells = 20, float duration = 0.8f)
    {
        _instance = new ShatterInstance(
            position, strength, numCells,
            Random.Shared.NextSingle() * 1000f,  // unique seed = unique crack pattern
            duration, 0f);
    }

    public void Update(GameTime gameTime)
    {
        if (!_instance.HasValue) return;
        var inst = _instance.Value;
        inst = inst with { Age = inst.Age + (float)gameTime.ElapsedGameTime.TotalSeconds };
        _instance = inst.Age >= inst.Duration ? null : inst;
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!_instance.HasValue) return;
        var inst = _instance.Value;

        float t       = inst.Age / inst.Duration;
        float shatter = MathF.Sin(t * MathF.PI);  // 0 → peak → 0

        var vp       = _graphicsDevice.Viewport;
        var originUV = new Vector2(inst.Position.X / vp.Width, inst.Position.Y / vp.Height);

        _pOrigin.SetValue(originUV);
        _pStrength.SetValue(inst.Strength);
        _pNumCells.SetValue((float)inst.NumCells);
        _pSeed.SetValue(inst.Seed);
        _pShatter.SetValue(shatter);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);
        _pSceneTexture.SetValue(source);

        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
```

- [ ] **Step 3: Register shader in Content.mgcb**

Add after the `Overlay_Smoke.fx` block:

```
#begin ../../ScreenFXBuddy/Content/Distorter_GlassShatter.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_GlassShatter.fx;Distorter_GlassShatter.fx
```

- [ ] **Step 4: Update ScreenFXComponent.cs**

**Add property** (after `Vortex`):

```csharp
public GlassShatterLayer GlassShatter { get; private set; }
```

**In `LoadContent`**, add:

```csharp
GlassShatter = new GlassShatterLayer(GraphicsDevice);
```

**Update `DistortionLayers.AddRange`** to include `GlassShatter`:

```csharp
DistortionLayers.AddRange(new IDistortionLayer[]
    { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze, FreezeFrame, ZoomBlur, ScreenTilt, Vortex, GlassShatter });
```

**Add trigger method**:

```csharp
public void TriggerGlassShatter(Vector2 position, float strength = 0.04f, int numCells = 20, float duration = 0.8f)
    => GlassShatter.Trigger(position, strength, numCells, duration);
```

- [ ] **Step 5: Add Game1.cs bindings**

```csharp
// OemQuestion: default glass shatter — 20 shards
if (keys.IsKeyDown(Keys.OemQuestion) && !_prevKeys.IsKeyDown(Keys.OemQuestion))
    _screenFX.TriggerGlassShatter(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f));

// NumPad0: many fine shards — strong impact on hard surface
if (keys.IsKeyDown(Keys.NumPad0) && !_prevKeys.IsKeyDown(Keys.NumPad0))
    _screenFX.TriggerGlassShatter(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        strength: 0.03f, numCells: 35, duration: 0.6f);

// NumPadDecimal: few large shards — slow dramatic break
if (keys.IsKeyDown(Keys.Decimal) && !_prevKeys.IsKeyDown(Keys.Decimal))
    _screenFX.TriggerGlassShatter(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        strength: 0.07f, numCells: 8, duration: 1.2f);
```

Note: In MonoGame, the numpad decimal key is `Keys.Decimal`, not `Keys.NumPadDecimal`.

- [ ] **Step 6: Verify and commit**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
dotnet run --project ScreenFXBuddy.Example
```

Test each key:
- **/**: Default glass shatter — screen cracks into ~20 shards that push out and return over 0.8s. Each press produces a different crack pattern (random seed).
- **NumPad0**: Many fine cracks, faster animation
- **NumPad.** (decimal): Few large dramatic shards, slower

If cracks are not visible, verify that the Voronoi loop runs — try temporarily hard-coding `NumCells = 5` and checking whether 5 cells are visible.

```bash
git add ScreenFXBuddy/Content/Distorter_GlassShatter.fx \
        ScreenFXBuddy/Effects/GlassShatterLayer.cs \
        ScreenFXBuddy/ScreenFXComponent.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb \
        ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: add GlassShatterLayer — procedural Voronoi cracks, sin(t·π) push-and-return"
```
