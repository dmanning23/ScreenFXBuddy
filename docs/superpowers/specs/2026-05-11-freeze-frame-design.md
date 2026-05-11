# FreezeFrameLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `FreezeFrameLayer` — a full-screen desaturation + vignette + configurable tint effect that makes the scene feel like time has stopped. When triggered, the scene drains toward grayscale, darkens at the edges, and shifts toward a chosen tint color (cold blue for ice, hot red for intensity, pink for a stylized hit). The effect fades in, holds, then returns to normal — matching the flash-hold-fade envelope of `AnimeSuperLayer`.

This is a new `IDistortionLayer` (it must sample and modify scene pixels — a pure overlay cannot desaturate). Single instance.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/FreezeFrameLayer.cs` | Manages single instance, uploads shader params |
| Create | `ScreenFXBuddy/Content/Distorter_FreezeFrame.fx` | Desaturation + vignette + tint shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `FreezeFrame` property and `TriggerFreezeFrame` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

---

## FreezeFrameLayer

New class. Implements `IDistortionLayer`.

**State fields:**
```csharp
private Vector4 _tintColor;  // pre-converted from XNA Color
private float   _flashIn;
private float   _hold;
private float   _fadeOut;
private float   _age;
private bool    _active;
```

**Cached EffectParameter handles:**
- `_pTintColor`  — `float4`
- `_pIntensity`  — `float` (0→1→0 over lifecycle)

`public bool IsActive => _active;`

**`LoadContent`:** loads `"Distorter_FreezeFrame"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Color tintColor,
    float flashIn  = 0.10f,
    float hold     = 0.40f,
    float fadeOut  = 0.30f)
```
Converts `tintColor` to `Vector4` via `.ToVector4()`. Sets all fields, `_age = 0`, `_active = true`. Always replaces any in-progress instance.

**`Update`:** increments `_age`, sets `_active = false` when `_age >= flashIn + hold + fadeOut`.

**`Apply`:**

Compute `intensity` using the same three-phase logic as `AnimeSuperLayer`:
```csharp
float intensity;
if (_age < _flashIn)
    intensity = _flashIn > 0 ? _age / _flashIn : 1f;
else if (_age < _flashIn + _hold)
    intensity = 1f;
else
    intensity = 1f - (_age - _flashIn - _hold) / _fadeOut;

intensity = MathHelper.Clamp(intensity, 0f, 1f);
```

Upload parameters, then single `spriteBatch.Begin/Draw/End`.

---

## Shader: Distorter_FreezeFrame.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene (bound by SpriteBatch) |
| `TintColor` | `float4` | RGB tint to shift toward (alpha unused) |
| `Intensity` | `float` | 0→1→0 animation value |

**Algorithm:**

```hlsl
float4 sceneColor = tex2D(SceneSampler, input.TexCoord);

// Step 1: Desaturate — luminance-weighted grayscale
float luma = dot(sceneColor.rgb, float3(0.299, 0.587, 0.114));
float3 gray = float3(luma, luma, luma);

// Step 2: Tint — lerp gray toward tint color
// At Intensity=1 the scene is fully gray-tinted; at 0 it's unchanged
float3 tinted = lerp(gray, gray * TintColor.rgb * 2.0, 0.5);
// (multiplying tint by 2 and lerping at 0.5 preserves luminance
//  while shifting hue toward the tint color)

// Step 3: Blend tinted result with original based on Intensity
float3 processed = lerp(sceneColor.rgb, tinted, Intensity);

// Step 4: Vignette — radial darkening at screen edges
float2 centered = input.TexCoord - float2(0.5, 0.5);
float vignetteDist = length(centered);
float vignette = 1.0 - smoothstep(0.35, 0.75, vignetteDist);
// Vignette darkens toward edges; scale by Intensity so it fades with effect
vignette = lerp(1.0, vignette, Intensity * 0.7);

float3 finalColor = processed * vignette;
return float4(finalColor, sceneColor.a) * input.Color;
```

**Technique:** `FreezeFrame`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

```csharp
public FreezeFrameLayer FreezeFrame { get; private set; }

public void TriggerFreezeFrame(
    Color tintColor,
    float flashIn  = 0.10f,
    float hold     = 0.40f,
    float fadeOut  = 0.30f)
    => FreezeFrame.Trigger(tintColor, flashIn, hold, fadeOut);
```

Register `FreezeFrame` in `DistortionLayers`.

---

## Example Project

```csharp
// F7: cold blue tint — ice super / time stop
if (keys.IsKeyDown(Keys.F7) && !_prevKeys.IsKeyDown(Keys.F7))
    _screenFX.TriggerFreezeFrame(new Color(100, 160, 255));

// F8: hot red tint — rage mode / critical hit
if (keys.IsKeyDown(Keys.F8) && !_prevKeys.IsKeyDown(Keys.F8))
    _screenFX.TriggerFreezeFrame(new Color(255, 80, 60),
        flashIn: 0f, hold: 0.3f, fadeOut: 0.5f);

// F9: pink tint — stylized "beautiful" super moment
if (keys.IsKeyDown(Keys.F9) && !_prevKeys.IsKeyDown(Keys.F9))
    _screenFX.TriggerFreezeFrame(new Color(255, 160, 200),
        flashIn: 0.15f, hold: 0.6f, fadeOut: 0.4f);
```

---

## Visual Design Notes

- **Tint formula:** `lerp(gray, gray * TintColor.rgb * 2.0, 0.5)` keeps the scene recognizable (it's still grayscale-based) while shifting its palette toward the tint hue. A pure `lerp(gray, TintColor, t)` would replace scene detail with a flat color at high intensity — this formulation preserves contrast while coloring it.
- **Vignette strength:** capped at `Intensity * 0.7` so even at peak the edges are darkened, not blacked out. Adjust the `0.7` multiplier in the shader if more dramatic vignetting is desired.
- **`smoothstep(0.35, 0.75, vignetteDist)`:** vignette starts 35% from center and reaches full strength at 75%. On a 16:9 viewport the corners are at distance ~0.59, putting them solidly in the darkened zone.
- **Pairing:** `FreezeFrameLayer` is designed to run alongside `LetterboxLayer` and `AnimeSuperLayer` simultaneously. The distortion layer draws first (modifies the scene), then overlay layers draw on top. The ordering is handled by `ScreenFXComponent`'s existing layer processing.
- **`IDistortionLayer` rationale:** desaturation requires reading scene pixel colors and outputting modified values. An `IOverlayLayer` draws on top of the existing scene and cannot modify it — only a distortion layer (which receives the render target as input) can desaturate.
