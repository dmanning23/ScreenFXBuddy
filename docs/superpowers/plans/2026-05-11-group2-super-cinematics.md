# Group 2 — Super Cinematics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement three super-cinematic effects — AnimeSuperLayer (flash-hold-fade), LetterboxLayer (sliding black bars), and FreezeFrameLayer (desaturation + tint + vignette).

**Architecture:** AnimeSuperLayer completes the existing stub by removing the `Debug_Color` shader dependency and computing the flash-hold-fade envelope purely in C#. LetterboxLayer is a new pure-C# `IOverlayLayer` with a state machine. FreezeFrameLayer is a new `IDistortionLayer` that modifies scene pixels through a dedicated shader. All three are wired into `ScreenFXComponent` and tested via keybindings F1–F9.

**Tech Stack:** MonoGame, HLSL (FreezeFrame only), C# 12, `IOverlayLayer`, `IDistortionLayer`.

---

## Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `ScreenFXBuddy/Effects/AnimeSuperLayer.cs` | Replace Debug_Color shader stub with pure-C# flash-hold-fade |
| Create | `ScreenFXBuddy/Effects/LetterboxLayer.cs` | Sliding black bars, state machine, no shader |
| Create | `ScreenFXBuddy/Content/Distorter_FreezeFrame.fx` | Desaturation + tint + vignette shader |
| Create | `ScreenFXBuddy/Effects/FreezeFrameLayer.cs` | Flash-hold-fade distortion layer, single instance |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add Letterbox + FreezeFrame; update AnimeSuper signature |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register Distorter_FreezeFrame.fx |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Replace D7 binding; add F1–F9 bindings |

---

### Task 1: Complete AnimeSuperLayer

**Spec:** `docs/superpowers/specs/2026-05-11-anime-super-design.md`

**Files:**
- Modify: `ScreenFXBuddy/Effects/AnimeSuperLayer.cs`

Context: The stub loads `Debug_Color` shader and does a simple linear fade-out. The spec removes the shader entirely and replaces with a three-phase flash-hold-fade envelope computed in C#. The `Trigger` signature changes from `(Color, float duration)` to `(Color, float flashIn, float hold, float fadeOut)`.

- [ ] **Step 1: Replace AnimeSuperLayer.cs with the full implementation**

Replace the entire file with:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class AnimeSuperLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _whitePixel;

    private Color _color;
    private float _flashIn;
    private float _hold;
    private float _fadeOut;
    private float _age;
    private bool _active;

    public bool IsActive => _active;

    public AnimeSuperLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="color">Flash color. White = pure blinding flash; use tints for themed supers.</param>
    /// <param name="flashIn">Seconds to ramp from 0 → full alpha.</param>
    /// <param name="hold">Seconds at full alpha before fading.</param>
    /// <param name="fadeOut">Seconds to fade from full alpha → 0.</param>
    public void Trigger(Color color, float flashIn = 0.05f, float hold = 0.30f, float fadeOut = 0.40f)
    {
        _color   = color;
        _flashIn = flashIn;
        _hold    = hold;
        _fadeOut = fadeOut;
        _age     = 0f;
        _active  = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!_active) return;
        _age += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_age >= _flashIn + _hold + _fadeOut)
            _active = false;
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        float alpha;
        if (_age < _flashIn)
            alpha = _flashIn > 0f ? _age / _flashIn : 1f;
        else if (_age < _flashIn + _hold)
            alpha = 1f;
        else
        {
            float fadeProgress = _age - _flashIn - _hold;
            alpha = _fadeOut > 0f ? 1f - fadeProgress / _fadeOut : 0f;
        }

        var vp = _graphicsDevice.Viewport;
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        spriteBatch.Draw(_whitePixel, vp.Bounds, _color * alpha);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: Build succeeded, 0 errors. (It will warn about the `TriggerAnimeSuper` call-site in `ScreenFXComponent.cs` not matching — ignore for now, that's fixed in Task 5.)

Actually `ScreenFXBuddy.csproj` alone won't complain about call-sites. But if you run the Example project it will. Build just the library project for now.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/AnimeSuperLayer.cs
git commit -m "feat: complete AnimeSuperLayer — flash-hold-fade envelope, no shader"
```

---

### Task 2: Create LetterboxLayer

**Spec:** `docs/superpowers/specs/2026-05-11-letterbox-design.md`

**Files:**
- Create: `ScreenFXBuddy/Effects/LetterboxLayer.cs`

Context: Pure C#, no shader. Draws two black rectangles (top and bottom) whose height slides in and out based on a state machine. Uses `BlendState.AlphaBlend` since we want opaque black bars.

- [ ] **Step 1: Create LetterboxLayer.cs**

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class LetterboxLayer : IOverlayLayer, IDisposable
{
    private enum State { Idle, SlidingIn, Holding, SlidingOut }

    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _blackPixel;

    private State _state = State.Idle;
    private float _barHeight;   // fraction of screen height (0.10 = 10%)
    private float _slideIn;
    private float _hold;
    private float _slideOut;
    private float _stateAge;    // time elapsed in current state

    public bool IsActive => _state != State.Idle;

    public LetterboxLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _blackPixel = new Texture2D(_graphicsDevice, 1, 1);
        _blackPixel.SetData(new[] { Color.Black });
    }

    /// <param name="barHeight">Bar height as fraction of screen height. 0.10 = 10% each bar.</param>
    /// <param name="slideIn">Seconds for bars to slide in to full height.</param>
    /// <param name="hold">Seconds bars stay at full height.</param>
    /// <param name="slideOut">Seconds for bars to slide back out.</param>
    public void Trigger(float barHeight = 0.10f, float slideIn = 0.15f, float hold = 1.00f, float slideOut = 0.15f)
    {
        _barHeight = barHeight;
        _slideIn   = slideIn;
        _hold      = hold;
        _slideOut  = slideOut;
        _stateAge  = 0f;
        _state     = State.SlidingIn;
    }

    public void Update(GameTime gameTime)
    {
        if (_state == State.Idle) return;
        _stateAge += (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case State.SlidingIn when _stateAge >= _slideIn:
                _state    = State.Holding;
                _stateAge -= _slideIn;
                break;
            case State.Holding when _stateAge >= _hold:
                _state    = State.SlidingOut;
                _stateAge -= _hold;
                break;
            case State.SlidingOut when _stateAge >= _slideOut:
                _state = State.Idle;
                break;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        float currentFraction = _state switch
        {
            State.SlidingIn  => _slideIn  > 0f ? _barHeight * (_stateAge / _slideIn)             : _barHeight,
            State.Holding    => _barHeight,
            State.SlidingOut => _slideOut > 0f ? _barHeight * (1f - _stateAge / _slideOut) : 0f,
            _                => 0f
        };

        if (currentFraction <= 0f) return;

        var vp = _graphicsDevice.Viewport;
        int barPixels = (int)(currentFraction * vp.Height);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        spriteBatch.Draw(_blackPixel, new Rectangle(0, 0, vp.Width, barPixels), Color.White);
        spriteBatch.Draw(_blackPixel, new Rectangle(0, vp.Height - barPixels, vp.Width, barPixels), Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _blackPixel?.Dispose();
    }
}
```

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/LetterboxLayer.cs
git commit -m "feat: add LetterboxLayer — sliding black bars, pure C# state machine"
```

---

### Task 3: Create FreezeFrame shader

**Spec:** `docs/superpowers/specs/2026-05-11-freeze-frame-design.md`

**Files:**
- Create: `ScreenFXBuddy/Content/Distorter_FreezeFrame.fx`
- Modify: `ScreenFXBuddy.Example/Content/Content.mgcb`

Context: The shader desaturates the scene, applies a configurable tint to the gray image, and adds a vignette darkening at the screen edges. It receives an `Intensity` float (0→1) that controls how much effect is applied. The `#if OPENGL` compatibility block is required — without it the shader silently renders black on macOS DesktopGL. Return semantic must be `: COLOR` not `: COLOR0` or `: SV_Target`. Shader compilation on macOS requires `MGFXC_WINE_PATH=/Users/danmanning/.winemonogame`.

- [ ] **Step 1: Create Distorter_FreezeFrame.fx**

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

float4 TintColor;    // RGBA — e.g. (0.39, 0.63, 1.0, 1.0) for icy blue
float  Intensity;    // 0→1: how strongly the effect is applied
float  AspectRatio;  // width / height, for circular vignette

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;

    // Aspect-corrected distance from screen center (makes vignette circular)
    float2 offset = float2((uv.x - 0.5) * AspectRatio, uv.y - 0.5);
    float  dist   = length(offset);

    float4 original = tex2D(SceneSampler, uv);

    // Desaturate to grayscale
    float  luma = dot(original.rgb, float3(0.299, 0.587, 0.114));
    float3 gray = float3(luma, luma, luma);

    // Tint shift: blend 50/50 between gray and tinted gray
    // Multiplying by 2 compensates for the 0.5 blend factor so full-white
    // tint leaves the image as pure gray.
    float3 tinted = lerp(gray, gray * TintColor.rgb * 2.0, 0.5);

    // Vignette: radial darkening toward screen edges, scaled by Intensity
    float vignette  = smoothstep(0.35, 0.75, dist) * Intensity * 0.7;
    float3 vignetted = tinted * (1.0 - vignette);

    // Blend between original and fully-processed based on Intensity
    float3 result = lerp(original.rgb, vignetted, Intensity);

    return float4(result, original.a) * input.Color;
}

technique FreezeFrame
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

- [ ] **Step 2: Register the shader in Content.mgcb**

Add these lines after the `Distorter_GravityWave.fx` block in `ScreenFXBuddy.Example/Content/Content.mgcb`:

```
#begin ../../ScreenFXBuddy/Content/Distorter_FreezeFrame.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:../../ScreenFXBuddy/Content/Distorter_FreezeFrame.fx;Distorter_FreezeFrame.fx
```

- [ ] **Step 3: Build to verify shader compiles**

```bash
cd /Users/danmanning/Documents/Source/ScreenFXBuddy
MGFXC_WINE_PATH=/Users/danmanning/.winemonogame dotnet build ScreenFXBuddy.Example
```

Expected: Build succeeded, 0 errors. If the shader fails, common causes:
- Wrong return semantic (must be `: COLOR`, not `: COLOR0` or `: SV_Target`)
- Missing `#if OPENGL` block
- Syntax error in HLSL

- [ ] **Step 4: Commit**

```bash
git add ScreenFXBuddy/Content/Distorter_FreezeFrame.fx ScreenFXBuddy.Example/Content/Content.mgcb
git commit -m "feat: add Distorter_FreezeFrame.fx — desaturation, tint, vignette shader"
```

---

### Task 4: Create FreezeFrameLayer

**Spec:** `docs/superpowers/specs/2026-05-11-freeze-frame-design.md`

**Files:**
- Create: `ScreenFXBuddy/Effects/FreezeFrameLayer.cs`

Context: `IDistortionLayer` — its `Apply` takes `(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)`. It must `SetRenderTarget(destination)` before drawing. The effect renders from `source` into `destination` with the freeze-frame shader active. The `Intensity` parameter drives the flash-hold-fade envelope (0 at start/end, 1 during hold). No `IDisposable` needed — no directly-created textures; the effect is disposed by `ContentManager`.

- [ ] **Step 1: Create FreezeFrameLayer.cs**

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class FreezeFrameLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pTintColor;
    private EffectParameter _pIntensity;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private Vector4 _tintColor;
    private float   _flashIn;
    private float   _hold;
    private float   _fadeOut;
    private float   _age;
    private bool    _active;

    public bool IsActive => _active;

    public FreezeFrameLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_FreezeFrame");
        _pTintColor    = _effect.Parameters["TintColor"];
        _pIntensity    = _effect.Parameters["Intensity"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="tintColor">Color to tint the desaturated scene toward. (100,160,255) = icy blue.</param>
    /// <param name="flashIn">Seconds to ramp from no effect → full effect.</param>
    /// <param name="hold">Seconds at full effect intensity.</param>
    /// <param name="fadeOut">Seconds to fade back to normal.</param>
    public void Trigger(Color tintColor, float flashIn = 0.10f, float hold = 0.40f, float fadeOut = 0.30f)
    {
        _tintColor = tintColor.ToVector4();
        _flashIn   = flashIn;
        _hold      = hold;
        _fadeOut   = fadeOut;
        _age       = 0f;
        _active    = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!_active) return;
        _age += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_age >= _flashIn + _hold + _fadeOut)
            _active = false;
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        float intensity;
        if (_age < _flashIn)
            intensity = _flashIn > 0f ? _age / _flashIn : 1f;
        else if (_age < _flashIn + _hold)
            intensity = 1f;
        else
        {
            float fadeProgress = _age - _flashIn - _hold;
            intensity = _fadeOut > 0f ? 1f - fadeProgress / _fadeOut : 0f;
        }

        var vp = _graphicsDevice.Viewport;
        _pTintColor.SetValue(_tintColor);
        _pIntensity.SetValue(intensity);
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

- [ ] **Step 2: Verify it compiles**

```bash
dotnet build ScreenFXBuddy/ScreenFXBuddy.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add ScreenFXBuddy/Effects/FreezeFrameLayer.cs
git commit -m "feat: add FreezeFrameLayer — desaturation + tint + vignette, flash-hold-fade"
```

---

### Task 5: Wire up ScreenFXComponent and Game1.cs

**Files:**
- Modify: `ScreenFXBuddy/ScreenFXComponent.cs`
- Modify: `ScreenFXBuddy.Example/Game1.cs`

Context: `ScreenFXComponent` currently has `TriggerAnimeSuper(Color color, float duration)` — the 2-parameter old signature. This needs to become the new 4-parameter signature. `LetterboxLayer` goes in `OverlayLayers`. `FreezeFrameLayer` goes in `DistortionLayers`. In `Game1.cs`, the `D7` key currently calls `TriggerAnimeSuper(Color.White, 1f)` with the old signature — replace it with F1 bindings.

- [ ] **Step 1: Update ScreenFXComponent.cs**

**Add two new properties** (after line 28, with the other `public X { get; private set; }` declarations):

```csharp
public LetterboxLayer Letterbox { get; private set; }
public FreezeFrameLayer FreezeFrame { get; private set; }
```

**In `LoadContent`, after line 47** (`AnimeSuper = new AnimeSuperLayer(GraphicsDevice);`), add:

```csharp
Letterbox   = new LetterboxLayer(GraphicsDevice);
FreezeFrame = new FreezeFrameLayer(GraphicsDevice);
```

**Update the `DistortionLayers.AddRange` call** (line 50) to include `FreezeFrame`:

```csharp
DistortionLayers.AddRange(new IDistortionLayer[]
    { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze, FreezeFrame });
```

**Update the `OverlayLayers.AddRange` call** (line 52) to include `Letterbox`:

```csharp
OverlayLayers.AddRange(new IOverlayLayer[]
    { HitFlash, AnimeSuper, Letterbox, SpeedLines });
```

**Replace the `TriggerAnimeSuper` method** (line 137–138):

```csharp
public void TriggerAnimeSuper(Color color, float flashIn = 0.05f, float hold = 0.30f, float fadeOut = 0.40f)
    => AnimeSuper.Trigger(color, flashIn, hold, fadeOut);
```

**Add new trigger methods** (after `TriggerAnimeSuper`):

```csharp
public void TriggerLetterbox(float barHeight = 0.10f, float slideIn = 0.15f, float hold = 1.00f, float slideOut = 0.15f)
    => Letterbox.Trigger(barHeight, slideIn, hold, slideOut);

public void TriggerFreezeFrame(Color tintColor, float flashIn = 0.10f, float hold = 0.40f, float fadeOut = 0.30f)
    => FreezeFrame.Trigger(tintColor, flashIn, hold, fadeOut);
```

- [ ] **Step 2: Update Game1.cs**

**Replace the D7 AnimeSuper binding** (the old `TriggerAnimeSuper(Color.White, 1f)` call). Find the line:

```csharp
if (keys.IsKeyDown(Keys.D7) && !_prevKeys.IsKeyDown(Keys.D7))
    _screenFX.TriggerAnimeSuper(Color.White, 1f);
```

Replace with F1/F2/F3 bindings for AnimeSuper, F4/F5/F6 for Letterbox, F7/F8/F9 for FreezeFrame:

```csharp
// F1: white flash — standard super flash
if (keys.IsKeyDown(Keys.F1) && !_prevKeys.IsKeyDown(Keys.F1))
    _screenFX.TriggerAnimeSuper(Color.White);

// F2: red flash — rage super
if (keys.IsKeyDown(Keys.F2) && !_prevKeys.IsKeyDown(Keys.F2))
    _screenFX.TriggerAnimeSuper(new Color(255, 80, 80), flashIn: 0.08f, hold: 0.50f, fadeOut: 0.60f);

// F3: gold flash — ultra / power super
if (keys.IsKeyDown(Keys.F3) && !_prevKeys.IsKeyDown(Keys.F3))
    _screenFX.TriggerAnimeSuper(new Color(255, 220, 80), flashIn: 0.12f, hold: 0.80f, fadeOut: 1.00f);

// F4: default letterbox — cinematic super intro
if (keys.IsKeyDown(Keys.F4) && !_prevKeys.IsKeyDown(Keys.F4))
    _screenFX.TriggerLetterbox();

// F5: slow dramatic letterbox
if (keys.IsKeyDown(Keys.F5) && !_prevKeys.IsKeyDown(Keys.F5))
    _screenFX.TriggerLetterbox(barHeight: 0.12f, slideIn: 0.30f, hold: 2.00f, slideOut: 0.30f);

// F6: quick snap letterbox
if (keys.IsKeyDown(Keys.F6) && !_prevKeys.IsKeyDown(Keys.F6))
    _screenFX.TriggerLetterbox(barHeight: 0.08f, slideIn: 0.05f, hold: 0.50f, slideOut: 0.20f);

// F7: icy blue freeze frame — cryo special
if (keys.IsKeyDown(Keys.F7) && !_prevKeys.IsKeyDown(Keys.F7))
    _screenFX.TriggerFreezeFrame(new Color(100, 160, 255));

// F8: red freeze frame — rage / danger moment
if (keys.IsKeyDown(Keys.F8) && !_prevKeys.IsKeyDown(Keys.F8))
    _screenFX.TriggerFreezeFrame(new Color(255, 100, 100), flashIn: 0.05f, hold: 0.60f, fadeOut: 0.40f);

// F9: white freeze frame — dramatic finish
if (keys.IsKeyDown(Keys.F9) && !_prevKeys.IsKeyDown(Keys.F9))
    _screenFX.TriggerFreezeFrame(Color.White, flashIn: 0.15f, hold: 0.80f, fadeOut: 0.50f);
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
- **F1**: Bright white flash, short hold, medium fade (default super flash)
- **F2**: Red flash, longer hold and fade (rage super)
- **F3**: Gold flash, slowest envelope (ultra)
- **F4**: Black bars slide in from top/bottom, hold, slide back out
- **F5**: Slower/taller bars with longer hold
- **F6**: Quick snap bars (barely visible slide, short hold)
- **F7**: Scene desaturates with blue tint + vignette, flashes in and fades
- **F8**: Red tinted desaturation, longer hold
- **F9**: White tint desaturation, slowest envelope

If F7–F9 show no effect (scene unchanged), the shader is not applying — check the debug sequence: replace the PS body with `return float4(1, 0, 0, 1);` to verify the shader is active, then re-add the logic.

- [ ] **Step 5: Commit**

```bash
git add ScreenFXBuddy/ScreenFXComponent.cs ScreenFXBuddy.Example/Game1.cs
git commit -m "feat: wire up AnimeSuperLayer, LetterboxLayer, FreezeFrameLayer — F1-F9 bindings"
```
