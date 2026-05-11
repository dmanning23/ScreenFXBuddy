# Group 1 — Impact Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement three impact-feedback effects — ZoomBlurLayer (radial UV push from a point), ChromaticSplit extension (transient channel-split mode on the existing layer), and ScreenTiltLayer (whole-scene rotation snap-and-ease-back).

**Architecture:** ZoomBlur and ScreenTilt are new `IDistortionLayer` classes each with their own HLSL shader. ChromaticSplit is a pure C# extension to the existing `ChromaticAberrationLayer` — no shader changes needed (the existing shader already accepts `Distance` and `Strength` as independent per-frame floats). All three are wired into `ScreenFXComponent` and tested via keybindings F10–F12 and page/end keys.

**Tech Stack:** MonoGame, HLSL (ZoomBlur + ScreenTilt), C# 12, `IDistortionLayer`.

---

## Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Content/Distorter_ZoomBlur.fx` | Distance-proportional radial UV push |
| Create | `ScreenFXBuddy/Effects/ZoomBlurLayer.cs` | Single-instance distortion, sin(t·π) curve |
| Modify | `ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs` | Add `AberrationMode` enum + `TriggerSplit` method |
| Create | `ScreenFXBuddy/Content/Distorter_ScreenTilt.fx` | UV rotation around screen center |
| Create | `ScreenFXBuddy/Effects/ScreenTiltLayer.cs` | Single-instance, snap-and-ease-back curve |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add ZoomBlur, ScreenTilt; add TriggerChromaticSplit, TriggerZoomBlur, TriggerScreenTilt |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register ZoomBlur and ScreenTilt shaders |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add F10–F12, PageUp/PageDown/Home, End/Insert/Delete bindings |

---

### Task 1: Create ZoomBlur shader and layer

**Spec:** `docs/superpowers/specs/2026-05-11-zoom-blur-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Distorter_ZoomBlur.fx`
- Create: `ScreenFXBuddy/Effects/ZoomBlurLayer.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: The shader computes a direction vector from the UV to the origin, scales it by distance × `Strength`, and adds the result to the UV as a displacement. `Strength` is set to `peakStrength * sin(t * π)` in C# before upload — so the shader itself is stateless; the curve lives entirely in C#. The distortion is only applied within `Radius` UV units of the origin.

- [ ] **Step 1: Create Distorter_ZoomBlur.fx**

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

float2 Origin;      // UV-space origin of the blur
float  Strength;    // pre-faded by C# (peakStrength * sin(t * π))
float  Radius;      // UV-space outer edge of the effect
float  AspectRatio; // width / height, for circular falloff region

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv     = input.TexCoord;
    float2 offset = uv - Origin;

    // Aspect-corrected distance so the affected region is circular
    float dist = length(float2(offset.x * AspectRatio, offset.y));

    if (dist > Radius || dist < 0.0001)
        return tex2D(SceneSampler, uv) * input.Color;

    // Radial envelope: full strength inside Radius*0.5, tapers off at edge
    float radialFade = 1.0 - smoothstep(Radius * 0.5, Radius, dist);

    // Push UV away from origin, proportional to distance (further = more push)
    float2 dir         = offset / dist;
    float2 displacement = dir * dist * Strength * radialFade;
    float2 sampleUV    = clamp(uv + displacement, 0.0, 1.0);

    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique ZoomBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create ZoomBlurLayer.cs**

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ZoomBlurLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pOrigin;
    private EffectParameter _pStrength;
    private EffectParameter _pRadius;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private record struct BlurInstance(
        Vector2 Origin,
        float PeakStrength,
        float Radius,
        float Duration,
        float Age);

    private BlurInstance? _instance;

    public bool IsActive => _instance.HasValue;

    public ZoomBlurLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_ZoomBlur");
        _pOrigin       = _effect.Parameters["Origin"];
        _pStrength     = _effect.Parameters["Strength"];
        _pRadius       = _effect.Parameters["Radius"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="position">Pixel-space position the blur radiates from.</param>
    /// <param name="strength">Peak UV displacement. 0.05 is subtle; 0.15 is dramatic.</param>
    /// <param name="radius">UV-space radius of affected area. 1.0 = full screen.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.05f, float radius = 1.0f, float duration = 0.4f)
    {
        _instance = new BlurInstance(position, strength, radius, duration, 0f);
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

        var vp = _graphicsDevice.Viewport;
        float t = inst.Age / inst.Duration;

        // sin(t * π) gives 0 → peak → 0: push-and-return motion
        float currentStrength = inst.PeakStrength * MathF.Sin(t * MathF.PI);

        var originUV = new Vector2(inst.Origin.X / vp.Width, inst.Origin.Y / vp.Height);

        _pOrigin.SetValue(originUV);
        _pStrength.SetValue(currentStrength);
        _pRadius.SetValue(inst.Radius);
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

Add after the `Distorter_FreezeFrame.fx` block in `ScreenFXBuddy.Example/Content/Content.mgcb`:

```
#begin ../../ScreenFXBuddy/Content/Distorter_ZoomBlur.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_ZoomBlur.fx;Distorter_ZoomBlur.fx
```

- [ ] **Step 4: Verify shader compiles**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/Content/Distorter_ZoomBlur.fx \
        ScreenFXBuddy/Effects/ZoomBlurLayer.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: add ZoomBlurLayer — radial push shader with sin(t·π) curve"
```

---

### Task 2: Extend ChromaticAberrationLayer with Split mode

**Spec:** `docs/superpowers/specs/2026-05-11-chromatic-split-design.md`

**Files:**
- Modify: `ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs`

Context: No shader changes. The existing `Distorter_ChromaticAberration.fx` already takes `Distance` and `Strength` as per-frame floats. The new `TriggerSplit` mode drives them with a `sin(t * π)` arc so both start and end at zero — no pop, no residual color shift. The two modes share all state fields; a new `_mode` enum field selects which Update/Apply path runs.

Read the current file before editing: `ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs`

- [ ] **Step 1: Add the new fields to ChromaticAberrationLayer.cs**

After the existing `private float _distance;` field, add:

```csharp
private enum AberrationMode { Sustained, Split }
private AberrationMode _mode = AberrationMode.Sustained;

// Split-mode state (unused in Sustained mode)
private float _splitMaxDistance;
private float _splitDuration;
private float _splitAge;
```

- [ ] **Step 2: Update IsActive to handle both modes**

Replace the existing `IsActive` property:

```csharp
public bool IsActive => _mode == AberrationMode.Sustained
    ? !Timer.Paused && Timer.HasTimeRemaining
    : _splitAge < _splitDuration;
```

- [ ] **Step 3: Add `_mode = AberrationMode.Sustained` to existing Trigger**

At the end of the existing `Trigger` method body (after `Timer.Start(time);`), add:

```csharp
_mode = AberrationMode.Sustained;
```

- [ ] **Step 4: Add the new TriggerSplit method**

Add this method after the existing `Trigger` method:

```csharp
/// <summary>
/// Transient chromatic split — channels fly apart and snap back.
/// Replaces any active Sustained aberration.
/// </summary>
/// <param name="position">Screen-pixel position the split radiates from.</param>
/// <param name="maxDistance">Peak UV-space channel separation. Try 0.03–0.08.</param>
/// <param name="duration">Total lifetime in seconds. Try 0.2–0.4 for a snappy hit.</param>
public void TriggerSplit(Vector2 position, float maxDistance = 0.05f, float duration = 0.3f)
{
    _startPosition    = position;
    _splitMaxDistance = maxDistance;
    _splitDuration    = duration;
    _splitAge         = 0f;
    _mode             = AberrationMode.Split;
}
```

- [ ] **Step 5: Update Update() to advance _splitAge in Split mode**

In the `Update` method, after `Timer.Update(gameTime);`, add:

```csharp
if (_mode == AberrationMode.Split)
    _splitAge = Math.Min(_splitAge + (float)gameTime.ElapsedGameTime.TotalSeconds, _splitDuration);
```

- [ ] **Step 6: Update Apply() to branch on mode**

In the `Apply` method, replace the two lines that compute `currentDistance` and `currentStrength`:

```csharp
// Distance grows from 0 → _distance as the timer counts down.
float currentDistance = _distance * ApplyCurve(1f - Timer.Lerp);

// Strength fades from 1 → 0 as the timer counts down.
float currentStrength = ApplyCurve(Timer.Lerp);
```

Replace with:

```csharp
float currentDistance, currentStrength;

if (_mode == AberrationMode.Sustained)
{
    // Existing behaviour — distance grows, strength fades
    currentDistance = _distance * ApplyCurve(1f - Timer.Lerp);
    currentStrength = ApplyCurve(Timer.Lerp);
}
else
{
    // Split mode — sin(t·π) arc: 0 → peak → 0
    float t = _splitDuration > 0f ? _splitAge / _splitDuration : 1f;
    float curve = MathF.Sin(t * MathF.PI);
    currentDistance = _splitMaxDistance * curve;
    currentStrength = curve;
}
```

- [ ] **Step 7: Verify it compiles**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs
git commit -m "feat: add ChromaticAberrationLayer Split mode — sin(t·π) transient channel split"
```

---

### Task 3: Create ScreenTilt shader and layer

**Spec:** `docs/superpowers/specs/2026-05-11-screen-tilt-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Distorter_ScreenTilt.fx`
- Create: `ScreenFXBuddy/Effects/ScreenTiltLayer.cs`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: The shader rotates UV coordinates around the screen center. Aspect-ratio correction is applied before rotation (`offset.x *= AspectRatio`) and undone after (`rotated.x /= AspectRatio`) so the rotation appears circular on widescreen viewports. The C# layer computes the angle using a snap-and-ease-back curve: first 10% of duration ramps linearly from 0 → maxAngle; remaining 90% uses a quadratic ease from maxAngle → 0. The angle is passed to the shader in radians.

- [ ] **Step 1: Create Distorter_ScreenTilt.fx**

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

float Angle;       // current rotation in radians; positive = clockwise
float AspectRatio; // width / height, for circular rotation

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv     = input.TexCoord;
    float2 center = float2(0.5, 0.5);
    float2 offset = uv - center;

    // Scale to screen space so rotation is circular, not elliptical
    offset.x *= AspectRatio;

    // 2D rotation
    float cosA = cos(Angle);
    float sinA = sin(Angle);
    float2 rotated = float2(
        offset.x * cosA - offset.y * sinA,
        offset.x * sinA + offset.y * cosA
    );

    // Scale back to UV space
    rotated.x /= AspectRatio;

    float2 sampleUV = clamp(rotated + center, 0.0, 1.0);
    return tex2D(SceneSampler, sampleUV) * input.Color;
}

technique ScreenTilt
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Create ScreenTiltLayer.cs**

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ScreenTiltLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pAngle;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private record struct TiltInstance(
        float MaxAngle,  // degrees; positive = clockwise
        float Duration,
        float Age);

    private TiltInstance? _instance;

    // Fraction of duration used for the snap-in. The remaining (1 - SnapFraction)
    // is the quadratic ease-back.
    private const float SnapFraction = 0.1f;

    public bool IsActive => _instance.HasValue;

    public ScreenTiltLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_ScreenTilt");
        _pAngle        = _effect.Parameters["Angle"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="angle">Peak rotation in degrees. Positive = clockwise. Try 2–5 for subtle, 5–8 for dramatic.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(float angle = 3.0f, float duration = 0.4f)
    {
        _instance = new TiltInstance(angle, duration, 0f);
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

        float t = inst.Age / inst.Duration;

        float currentAngleDeg;
        if (t < SnapFraction)
        {
            // Linear snap in: 0 → maxAngle over first SnapFraction of duration
            currentAngleDeg = inst.MaxAngle * (t / SnapFraction);
        }
        else
        {
            // Quadratic ease back: maxAngle → 0 over remaining duration
            float easeT = (t - SnapFraction) / (1f - SnapFraction);
            currentAngleDeg = inst.MaxAngle * (1f - easeT * easeT);
        }

        var vp = _graphicsDevice.Viewport;
        _pAngle.SetValue(MathHelper.ToRadians(currentAngleDeg));
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

Add after the `Distorter_ZoomBlur.fx` block:

```
#begin ../../ScreenFXBuddy/Content/Distorter_ScreenTilt.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_ScreenTilt.fx;Distorter_ScreenTilt.fx
```

- [ ] **Step 4: Verify shader compiles**

```bash
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/Content/Distorter_ScreenTilt.fx \
        ScreenFXBuddy/Effects/ScreenTiltLayer.cs \
        ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: add ScreenTiltLayer — UV rotation with snap-and-ease-back curve"
```

---

### Task 4: Wire up ScreenFXComponent and Game1.cs

**Files:**
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`
- Modify: `ScreenFXBuddy.Example/Game1.cs`

Context: `ScreenFXComponent` needs three new public properties and three new trigger methods. Both `ZoomBlurLayer` and `ScreenTiltLayer` go in `DistortionLayers`. In `Game1.cs`, no existing keys need to be removed — the new keys (F10–F12, page/end cluster) don't conflict with anything.

- [ ] **Step 1: Update ScreenFXComponent.cs**

**Add two new properties** (after the `SpeedLines` property):

```csharp
public ZoomBlurLayer ZoomBlur { get; private set; }
public ScreenTiltLayer ScreenTilt { get; private set; }
```

**In `LoadContent`**, after the `SpeedLines = new SpeedLinesLayer(GraphicsDevice);` line, add:

```csharp
ZoomBlur   = new ZoomBlurLayer(GraphicsDevice);
ScreenTilt = new ScreenTiltLayer(GraphicsDevice);
```

**Update `DistortionLayers.AddRange`** to include the new layers (add at the end):

```csharp
DistortionLayers.AddRange(new IDistortionLayer[]
    { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze, FreezeFrame, ZoomBlur, ScreenTilt });
```

**Add trigger methods** after `TriggerFreezeFrame`:

```csharp
public void TriggerZoomBlur(Vector2 position, float strength = 0.05f, float radius = 1.0f, float duration = 0.4f)
    => ZoomBlur.Trigger(position, strength, radius, duration);

public void TriggerChromaticSplit(Vector2 position, float maxDistance = 0.05f, float duration = 0.3f)
    => ChromaticAberration.TriggerSplit(position, maxDistance, duration);

public void TriggerScreenTilt(float angle = 3.0f, float duration = 0.4f)
    => ScreenTilt.Trigger(angle, duration);
```

- [ ] **Step 2: Add F10–F12 / PageUp / PageDown / Home / End / Insert / Delete bindings to Game1.cs**

Add these key handlers in `Game1.Update`, after the existing bindings:

```csharp
// F10: default zoom blur — super landing radial push
if (keys.IsKeyDown(Keys.F10) && !_prevKeys.IsKeyDown(Keys.F10))
    _screenFX.TriggerZoomBlur(centerPixels);

// F11: strong zoom blur — massive impact
if (keys.IsKeyDown(Keys.F11) && !_prevKeys.IsKeyDown(Keys.F11))
    _screenFX.TriggerZoomBlur(centerPixels, strength: 0.12f, radius: 1.0f, duration: 0.5f);

// F12: tight zoom blur — localized hit effect
if (keys.IsKeyDown(Keys.F12) && !_prevKeys.IsKeyDown(Keys.F12))
    _screenFX.TriggerZoomBlur(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        strength: 0.08f, radius: 0.5f, duration: 0.3f);

// PageUp: default chromatic split — snappy hit flash
if (keys.IsKeyDown(Keys.PageUp) && !_prevKeys.IsKeyDown(Keys.PageUp))
    _screenFX.TriggerChromaticSplit(centerPixels);

// PageDown: wide slow split — super impact
if (keys.IsKeyDown(Keys.PageDown) && !_prevKeys.IsKeyDown(Keys.PageDown))
    _screenFX.TriggerChromaticSplit(centerPixels, maxDistance: 0.09f, duration: 0.5f);

// Home: tight fast split — light hit
if (keys.IsKeyDown(Keys.Home) && !_prevKeys.IsKeyDown(Keys.Home))
    _screenFX.TriggerChromaticSplit(centerPixels, maxDistance: 0.025f, duration: 0.2f);

// End: default tilt — clockwise recoil
if (keys.IsKeyDown(Keys.End) && !_prevKeys.IsKeyDown(Keys.End))
    _screenFX.TriggerScreenTilt();

// Insert: heavy tilt — massive hit
if (keys.IsKeyDown(Keys.Insert) && !_prevKeys.IsKeyDown(Keys.Insert))
    _screenFX.TriggerScreenTilt(angle: 6.0f, duration: 0.5f);

// Delete: counter-clockwise tilt — hit from the other side
if (keys.IsKeyDown(Keys.Delete) && !_prevKeys.IsKeyDown(Keys.Delete))
    _screenFX.TriggerScreenTilt(angle: -3.0f, duration: 0.4f);
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
- **F10**: Gentle radial push from center, pushes-and-returns
- **F11**: Strong radial push, more dramatic
- **F12**: Tighter radius, localized around upper-center
- **PageUp**: RGB channels briefly separate and snap back (fast, snappy)
- **PageDown**: Wider separation, slower return
- **Home**: Subtle, tight separation (light hit feel)
- **End**: Scene rotates ~3° clockwise then eases back
- **Insert**: Scene rotates ~6° clockwise then eases back
- **Delete**: Scene rotates 3° counter-clockwise then eases back

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/ScreenFXComponent.cs ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: wire ZoomBlur, ChromaticSplit, ScreenTilt — F10-F12, page/end cluster"
```
