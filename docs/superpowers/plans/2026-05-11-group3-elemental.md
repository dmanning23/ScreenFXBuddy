# Group 3 — Elemental Effects Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement three elemental overlay/distortion effects — ElectricLayer (FBM polar-noise arcing tendrils), FrostLayer (FBM crystal overlay, radial expansion), and VortexLayer (inverse-distance UV rotation swirl).

**Architecture:** Electric and Frost are new `IOverlayLayer, IDisposable` classes drawn with additive blend on top of the back buffer. Electric supports 4 simultaneous instances with one draw call per instance (same pattern as `SpeedLinesLayer`). Frost is single-instance fire-and-forget. Vortex is a new `IDistortionLayer` with 4 simultaneous instances packed into `float4[]` arrays uploaded once per frame (same pattern as `GravityWaveLayer`).

**Tech Stack:** MonoGame, HLSL (all three), FBM value noise (Electric + Frost), packed shader arrays (Vortex), C# 12.

---

## Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Content/Overlay_Electric.fx` | FBM polar-noise tendrils, time-animated |
| Create | `ScreenFXBuddy/Effects/ElectricLayer.cs` | 4 instances, one draw call per instance |
| Create | `ScreenFXBuddy/Content/Overlay_Frost.fx` | FBM crystal overlay, no time animation |
| Create | `ScreenFXBuddy/Effects/FrostLayer.cs` | Single instance, radial expansion |
| Create | `ScreenFXBuddy/Content/Distorter_Vortex.fx` | Inverse-distance UV rotation |
| Create | `ScreenFXBuddy/Effects/VortexLayer.cs` | 4 instances, packed float4[] arrays |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add Electric, Frost, Vortex; add trigger methods |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register all three shaders |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add NumPad1–9 bindings |

---

### Task 1: Create ElectricLayer

**Spec:** `docs/superpowers/specs/2026-05-11-electric-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Overlay_Electric.fx`
- Create: `ScreenFXBuddy/Effects/ElectricLayer.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: The shader samples FBM using polar coordinates `(angle, dist)` to get radial spikes. The `Time` parameter (accumulated in C#) makes the tendrils flicker and crawl inward. The overbright core multiplier `1.0 + (1 - dist/Radius) * 0.8` creates a hot-spark feel at the center. Up to 4 instances; each gets its own `spriteBatch.Begin/Draw/End` call with additive blend. The `_time` field is global (shared across all instances) and accumulates monotonically.

- [ ] **Step 1: Create Overlay_Electric.fx**

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
float4 ElecColor;   // RGBA electricity tint
float  Radius;      // max spread radius (UV units)
float  Progress;    // 0→1 over lifetime
float  Time;        // accumulated time (drives flicker/crawl)
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
    float2 uv  = input.TexCoord;
    float2 off = float2((uv.x - Origin.x) * AspectRatio, uv.y - Origin.y);
    float  dist = length(off);

    if (dist > Radius * 1.1) return float4(0, 0, 0, 0);

    float angle = atan2(off.y, off.x);  // -π to +π

    // Polar FBM: angle axis creates angular spikes; dist axis stacks radially
    float2 noiseCoord = float2(
        angle * 4.0 / 3.14159 + Time * 3.0,  // ~4 spokes, fast flicker
        dist  * 12.0           - Time * 1.5   // inward crawl
    );
    float density  = fbm(noiseCoord);
    float tendrils = smoothstep(0.42, 0.62, density);

    float radialFade = 1.0 - smoothstep(Radius * 0.4, Radius, dist);

    // Ramp in over 20%, hold until 70%, ramp out over 30%
    float lifeFade = Progress < 0.2 ? Progress / 0.2
                   : Progress < 0.7 ? 1.0
                   : 1.0 - (Progress - 0.7) / 0.3;

    float alpha = tendrils * radialFade * lifeFade * ElecColor.a;

    // Overbright core: pixels close to origin appear hotter
    float brightness = 1.0 + (1.0 - dist / Radius) * 0.8;
    return float4(ElecColor.rgb * alpha * brightness, alpha);
}

technique Electric
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create ElectricLayer.cs**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ElectricLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;

    private EffectParameter _pOrigin;
    private EffectParameter _pElecColor;
    private EffectParameter _pRadius;
    private EffectParameter _pProgress;
    private EffectParameter _pTime;
    private EffectParameter _pAspectRatio;

    private record struct ElectricInstance(
        Vector2 Position,  // pixel-space origin
        Vector4 Color,     // RGBA pre-converted via .ToVector4()
        float Radius,
        float Duration,
        float Age);

    private const int MaxInstances = 4;
    private readonly List<ElectricInstance> _instances = new();
    private float _time;  // accumulated global time, drives flicker animation

    public bool IsActive => _instances.Count > 0;

    public ElectricLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Overlay_Electric");
        _pOrigin      = _effect.Parameters["Origin"];
        _pElecColor   = _effect.Parameters["ElecColor"];
        _pRadius      = _effect.Parameters["Radius"];
        _pProgress    = _effect.Parameters["Progress"];
        _pTime        = _effect.Parameters["Time"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="position">Pixel-space emitter position.</param>
    /// <param name="color">Electricity tint. (150,200,255) = blue-white; (180,80,255) = purple.</param>
    /// <param name="radius">Max spread radius in UV units. 0.20 = roughly a quarter-screen circle.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, Color color, float radius = 0.20f, float duration = 0.50f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new ElectricInstance(position, color.ToVector4(), radius, duration, 0f));
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
            float progress = inst.Age / inst.Duration;
            var uvOrigin = new Vector2(inst.Position.X / vp.Width, inst.Position.Y / vp.Height);

            _pOrigin.SetValue(uvOrigin);
            _pElecColor.SetValue(inst.Color);
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

Add after the `Distorter_ScreenTilt.fx` block in `ScreenFXBuddy.Example/Content/Content.mgcb`:

```
#begin ../../ScreenFXBuddy/Content/Overlay_Electric.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Overlay_Electric.fx;Overlay_Electric.fx
```

- [ ] **Step 4: Verify shader compiles**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/Content/Overlay_Electric.fx \
        ScreenFXBuddy/Effects/ElectricLayer.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: add ElectricLayer — FBM polar-noise tendrils, 4 instances, time-animated"
```

---

### Task 2: Create FrostLayer

**Spec:** `docs/superpowers/specs/2026-05-11-frost-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Overlay_Frost.fx`
- Create: `ScreenFXBuddy/Effects/FrostLayer.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: FBM is sampled using polar coordinates `(angle * 18 / π, dist * 6)` — same polar-FBM technique as Electric, with different scaling to produce wide thin facets around the ring and elongated needles along each spoke. No `Time` parameter — frost crystals don't animate once formed. The only animation is radial expansion (`frostRadius = Radius * min(Progress * 2, 1.0)`, reaching full size at Progress=0.5) and the lifetime fade-out (starts at 60% progress). A sparkle pass picks out the highest density noise peaks. Single instance — a new `Trigger` replaces any active instance.

- [ ] **Step 1: Create Overlay_Frost.fx**

```hlsl
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float2 Origin;      // UV-space origin of the frost
float4 TintColor;   // RGBA frost color; (0.7, 0.86, 1.0, 1.0) = icy blue-white
float  Radius;      // max spread radius (UV units)
float  Progress;    // 0→1 over lifetime
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
    float2 uv  = input.TexCoord;
    float2 off = float2((uv.x - Origin.x) * AspectRatio, uv.y - Origin.y);
    float  dist = length(off);

    // Frost expands over first half of lifetime, holds at full radius after
    float frostRadius = Radius * min(Progress * 2.0, 1.0);

    if (dist > frostRadius * 1.15) return float4(0, 0, 0, 0);

    // Polar crystal coordinates: sample FBM in (angle, dist) space.
    // angle * 18/π → many thin facets around the ring.
    // dist  * 6    → elongated needles along each spoke.
    float angle = atan2(off.y, off.x);
    float2 crystalCoord = float2(
        angle * 18.0 / 3.14159,
        dist  *  6.0
    );

    float density = fbm(crystalCoord);

    // Threshold: slightly softer band than Electric gives frosted-glass facets
    float crystals = smoothstep(0.38, 0.62, density);

    // Radial envelope: opaque at origin, soft at frost edge
    float radialFade = 1.0 - smoothstep(frostRadius * 0.5, frostRadius, dist);

    // Lifetime fade: hold until 60%, then fade out
    float lifeFade = Progress < 0.6 ? 1.0 : 1.0 - (Progress - 0.6) / 0.4;

    // Sparkle: extra brightness at highest-density noise peaks
    float sparkle = smoothstep(0.72, 0.85, density) * 0.5;

    float alpha = (crystals * 0.6 + sparkle) * radialFade * lifeFade * TintColor.a;
    return float4(TintColor.rgb * alpha, alpha);
}

technique Frost
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create FrostLayer.cs**

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class FrostLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;

    private EffectParameter _pOrigin;
    private EffectParameter _pTintColor;
    private EffectParameter _pRadius;
    private EffectParameter _pProgress;
    private EffectParameter _pAspectRatio;

    private Vector2 _origin;
    private Vector4 _tintColor;
    private float   _radius;
    private float   _duration;
    private float   _age;
    private bool    _active;

    public bool IsActive => _active;

    public FrostLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Overlay_Frost");
        _pOrigin      = _effect.Parameters["Origin"];
        _pTintColor   = _effect.Parameters["TintColor"];
        _pRadius      = _effect.Parameters["Radius"];
        _pProgress    = _effect.Parameters["Progress"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="position">Pixel-space origin of the frost spread.</param>
    /// <param name="tintColor">Frost color. (180,220,255) = icy blue; Color.White = pure white frost.</param>
    /// <param name="radius">Max spread radius in UV units. 0.25 = quarter-screen radius.</param>
    /// <param name="duration">Total effect duration in seconds. Frost is fully expanded at duration/2.</param>
    public void Trigger(Vector2 position, Color tintColor, float radius = 0.25f, float duration = 1.50f)
    {
        var vp  = _graphicsDevice.Viewport;
        _origin    = new Vector2(position.X / vp.Width, position.Y / vp.Height);
        _tintColor = tintColor.ToVector4();
        _radius    = radius;
        _duration  = duration;
        _age       = 0f;
        _active    = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!_active) return;
        _age += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_age >= _duration)
            _active = false;
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        float progress = MathHelper.Clamp(_age / _duration, 0f, 1f);
        var vp = _graphicsDevice.Viewport;

        _pOrigin.SetValue(_origin);
        _pTintColor.SetValue(_tintColor);
        _pRadius.SetValue(_radius);
        _pProgress.SetValue(progress);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(_whitePixel, vp.Bounds, Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
```

- [ ] **Step 3: Register shader in Content.mgcb**

Add after the `Overlay_Electric.fx` block:

```
#begin ../../ScreenFXBuddy/Content/Overlay_Frost.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Overlay_Frost.fx;Overlay_Frost.fx
```

- [ ] **Step 4: Verify shader compiles**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/Content/Overlay_Frost.fx \
        ScreenFXBuddy/Effects/FrostLayer.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: add FrostLayer — polar FBM crystal overlay, radial expansion, sparkle"
```

---

### Task 3: Create VortexLayer

**Spec:** `docs/superpowers/specs/2026-05-11-vortex-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Distorter_Vortex.fx`
- Create: `ScreenFXBuddy/Effects/VortexLayer.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: `IDistortionLayer`. Up to 4 simultaneous instances. Origins and state (swirl + radius) are packed into `float4[4]` arrays uploaded with a single `SetValue` call per frame — same pattern as `GravityWaveLayer`. In C#, `swirl = Strength * Speed * (1 - t)` bakes the direction (sign of Speed) and linear fade into a single float before upload. The shader reads the sign of `swirl` to determine rotation direction. Aspect-ratio correction ensures the vortex is circular: distance is measured in aspect-corrected space (`offset.x * AspectRatio`), rotation is performed in that space, then converted back. The clamp `max(dist, 0.04)` prevents extreme near-origin rotation.

- [ ] **Step 1: Create Distorter_Vortex.fx**

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

float  VortexCount;        // number of active instances (as float)
float4 VortexOrigins[4];   // .xy = (originX_uv, originY_uv)
float4 VortexState[4];     // .x = swirl (signed, pre-faded), .y = radius
float  AspectRatio;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;
    float2 totalDisplacement = float2(0.0, 0.0);
    int count = (int)VortexCount;

    for (int i = 0; i < count; i++)
    {
        float originX = VortexOrigins[i].x;
        float originY = VortexOrigins[i].y;
        float swirl   = VortexState[i].x;  // signed, pre-faded by C#
        float radius  = VortexState[i].y;

        if (abs(swirl) < 0.0001) continue;

        float2 offset = uv - float2(originX, originY);

        // Aspect-corrected distance for circular vortex shape
        float dist = length(float2(offset.x * AspectRatio, offset.y));

        if (dist > radius || dist < 0.001) continue;

        // Radial envelope: full swirl inside radius*0.6, tapers off toward edge
        float radialFade = 1.0 - smoothstep(radius * 0.6, radius, dist);

        // Swirl angle inversely proportional to distance (inner pixels rotate more)
        // Clamp prevents extreme near-origin rotation
        float swirlAngle = swirl / max(dist, 0.04) * radialFade;

        // Rotate in aspect-corrected space
        float cosA = cos(swirlAngle);
        float sinA = sin(swirlAngle);
        float2 aspectOffset = float2(offset.x * AspectRatio, offset.y);
        float2 rotated = float2(
            aspectOffset.x * cosA - aspectOffset.y * sinA,
            aspectOffset.x * sinA + aspectOffset.y * cosA
        );

        // Convert back to UV space and compute displacement
        float2 rotatedUV = float2(rotated.x / AspectRatio, rotated.y) + float2(originX, originY);
        totalDisplacement += rotatedUV - uv;
    }

    float2 sampleUV = clamp(uv + totalDisplacement, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique Vortex
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create VortexLayer.cs**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class VortexLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pVortexCount;
    private EffectParameter _pVortexOrigins;
    private EffectParameter _pVortexState;
    private EffectParameter _pAspectRatio;

    private record struct VortexInstance(
        Vector2 Position,  // pixel-space swirl center
        float Strength,    // peak swirl magnitude
        float Radius,      // UV-space outer edge
        float Speed,       // signed: positive = clockwise, negative = counter-clockwise
        float Duration,
        float Age);

    private const int MaxInstances = 4;
    private readonly List<VortexInstance> _instances = new();

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];

    public bool IsActive => _instances.Count > 0;

    public VortexLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect         = content.Load<Effect>("Distorter_Vortex");
        _pVortexCount   = _effect.Parameters["VortexCount"];
        _pVortexOrigins = _effect.Parameters["VortexOrigins"];
        _pVortexState   = _effect.Parameters["VortexState"];
        _pAspectRatio   = _effect.Parameters["AspectRatio"];
    }

    /// <param name="position">Pixel-space swirl center.</param>
    /// <param name="strength">Peak swirl magnitude in radians (at unit UV distance). Try 0.20–0.40.</param>
    /// <param name="radius">UV-space outer edge — pixels beyond this are unaffected.</param>
    /// <param name="speed">Signed multiplier. Positive = clockwise; negative = counter-clockwise.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.30f, float radius = 0.25f,
        float speed = 2.00f, float duration = 0.60f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new VortexInstance(position, strength, radius, speed, duration, 0f));
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
            float t     = inst.Age / inst.Duration;
            float swirl = inst.Strength * inst.Speed * (1f - t);  // linearly fades to zero; Speed sign = direction

            _originBuffer[i] = new Vector4(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height,
                0f, 0f);

            _stateBuffer[i] = new Vector4(swirl, inst.Radius, 0f, 0f);
        }

        _pVortexCount.SetValue((float)count);
        _pVortexOrigins.SetValue(_originBuffer);
        _pVortexState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);

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

Add after the `Overlay_Frost.fx` block:

```
#begin ../../ScreenFXBuddy/Content/Distorter_Vortex.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_Vortex.fx;Distorter_Vortex.fx
```

- [ ] **Step 4: Verify shader compiles**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/Content/Distorter_Vortex.fx \
        ScreenFXBuddy/Effects/VortexLayer.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: add VortexLayer — inverse-distance UV rotation, 4 instances, signed speed"
```

---

### Task 4: Wire up ScreenFXComponent and Game1.cs

**Files:**
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`
- Modify: `ScreenFXBuddy.Example/Game1.cs`

Context: `Electric` and `Frost` go in `OverlayLayers`; `Vortex` goes in `DistortionLayers`. NumPad1–3 for Electric, NumPad4–6 for Frost, NumPad7–9 for Vortex — none of these conflict with existing bindings.

- [ ] **Step 1: Update ScreenFXComponent.cs**

**Add three new properties** (after `ScreenTilt`):

```csharp
public ElectricLayer Electric { get; private set; }
public FrostLayer    Frost    { get; private set; }
public VortexLayer   Vortex   { get; private set; }
```

**In `LoadContent`**, after the `ScreenTilt = new ScreenTiltLayer(GraphicsDevice);` line:

```csharp
Electric = new ElectricLayer(GraphicsDevice);
Frost    = new FrostLayer(GraphicsDevice);
Vortex   = new VortexLayer(GraphicsDevice);
```

**Update `DistortionLayers.AddRange`** to include `Vortex` (add at the end):

```csharp
DistortionLayers.AddRange(new IDistortionLayer[]
    { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze, FreezeFrame, ZoomBlur, ScreenTilt, Vortex });
```

**Update `OverlayLayers.AddRange`** to include `Electric` and `Frost`:

```csharp
OverlayLayers.AddRange(new IOverlayLayer[]
    { HitFlash, AnimeSuper, Letterbox, SpeedLines, Electric, Frost });
```

**Add trigger methods** after `TriggerScreenTilt`:

```csharp
public void TriggerElectric(Vector2 position, Color color, float radius = 0.20f, float duration = 0.50f)
    => Electric.Trigger(position, color, radius, duration);

public void TriggerFrost(Vector2 position, Color tintColor, float radius = 0.25f, float duration = 1.50f)
    => Frost.Trigger(position, tintColor, radius, duration);

public void TriggerVortex(Vector2 position, float strength = 0.30f, float radius = 0.25f,
    float speed = 2.00f, float duration = 0.60f)
    => Vortex.Trigger(position, strength, radius, speed, duration);
```

- [ ] **Step 2: Add NumPad bindings to Game1.cs**

Add after the existing key handlers in `Game1.Update`:

```csharp
// NumPad1: blue-white electric — default shock
if (keys.IsKeyDown(Keys.NumPad1) && !_prevKeys.IsKeyDown(Keys.NumPad1))
    _screenFX.TriggerElectric(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(150, 200, 255));

// NumPad2: purple electricity — dark/shadow character
if (keys.IsKeyDown(Keys.NumPad2) && !_prevKeys.IsKeyDown(Keys.NumPad2))
    _screenFX.TriggerElectric(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(180, 80, 255), radius: 0.25f, duration: 0.7f);

// NumPad3: golden lightning — ki energy
if (keys.IsKeyDown(Keys.NumPad3) && !_prevKeys.IsKeyDown(Keys.NumPad3))
    _screenFX.TriggerElectric(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(255, 200, 60), radius: 0.15f, duration: 0.4f);

// NumPad4: icy blue frost — cryo special move
if (keys.IsKeyDown(Keys.NumPad4) && !_prevKeys.IsKeyDown(Keys.NumPad4))
    _screenFX.TriggerFrost(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(180, 220, 255));

// NumPad5: deep blue frost — heavy freeze
if (keys.IsKeyDown(Keys.NumPad5) && !_prevKeys.IsKeyDown(Keys.NumPad5))
    _screenFX.TriggerFrost(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(100, 160, 255), radius: 0.40f, duration: 2.0f);

// NumPad6: white frost — ice super, almost full screen
if (keys.IsKeyDown(Keys.NumPad6) && !_prevKeys.IsKeyDown(Keys.NumPad6))
    _screenFX.TriggerFrost(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        Color.White, radius: 0.55f, duration: 2.5f);

// NumPad7: clockwise vortex — air dash
if (keys.IsKeyDown(Keys.NumPad7) && !_prevKeys.IsKeyDown(Keys.NumPad7))
    _screenFX.TriggerVortex(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f));

// NumPad8: counter-clockwise vortex — teleport arrival
if (keys.IsKeyDown(Keys.NumPad8) && !_prevKeys.IsKeyDown(Keys.NumPad8))
    _screenFX.TriggerVortex(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        strength: 0.5f, radius: 0.35f, speed: -3.0f, duration: 0.5f);

// NumPad9: fast tight vortex — wind projectile
if (keys.IsKeyDown(Keys.NumPad9) && !_prevKeys.IsKeyDown(Keys.NumPad9))
    _screenFX.TriggerVortex(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        strength: 0.20f, radius: 0.15f, speed: 4.0f, duration: 0.35f);
```

- [ ] **Step 3: Build the full solution**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run and verify each effect**

```bash
dotnet run --project ScreenFXBuddy.Example
```

Test each key:
- **NumPad1**: Blue-white electric arcs radiate from upper-center, flicker and crawl inward
- **NumPad2**: Purple tendrils, slightly wider, longer duration
- **NumPad3**: Gold/yellow tendrils, tight radius, snappy duration
- **NumPad4**: Ice-blue frost crystals bloom outward from upper-center, linger, fade
- **NumPad5**: Deeper blue, larger radius
- **NumPad6**: White frost, near-full-screen, longest duration
- **NumPad7**: Scene pixels near upper-center rotate clockwise, tighter near center
- **NumPad8**: Counter-clockwise rotation, wider, teleport feel
- **NumPad9**: Fast tight clockwise, snappy

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/ScreenFXComponent.cs ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: wire Electric, Frost, Vortex — NumPad1-9 bindings"
```
