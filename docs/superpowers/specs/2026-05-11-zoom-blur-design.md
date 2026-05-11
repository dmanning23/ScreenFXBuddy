# ZoomBlurLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `ZoomBlurLayer` — a single-step radial UV push that makes the scene briefly "zoom out" from an impact point and snap back. Each pixel is displaced outward from the origin proportionally to its distance from it, creating a momentary zoom-shock on a heavy hit or super landing. Uses a push-and-return `sin(t * π)` curve identical to `GlassShatterLayer`.

This is a new `IDistortionLayer`, single instance.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/ZoomBlurLayer.cs` | Manages single instance, uploads shader params |
| Create | `ScreenFXBuddy/Content/Distorter_ZoomBlur.fx` | Radial UV push shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `ZoomBlur` property and `TriggerZoomBlur` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

---

## ZoomBlurInstance

```csharp
private record struct ZoomBlurInstance(
    Vector2 Position,   // pixel-space impact point
    float Strength,     // peak displacement magnitude in UV units
    float Radius,       // UV-space cap — pixels beyond this distance are unaffected (1.0 = full screen)
    float Duration,     // total lifetime in seconds
    float Age           // elapsed time in seconds
);
```

---

## ZoomBlurLayer

New class. Implements `IDistortionLayer`.

**Single instance:**
```csharp
private ZoomBlurInstance? _instance;
```

**Cached EffectParameter handles:**
- `_pOrigin`      — `float2`
- `_pStrength`    — `float`
- `_pRadius`      — `float`
- `_pAspectRatio` — `float`

`public bool IsActive => _instance.HasValue;`

**`LoadContent`:** loads `"Distorter_ZoomBlur"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    float strength = 0.05f,
    float radius   = 1.0f,
    float duration = 0.4f)
```
Always replaces any in-progress instance.

**`Update`:** increments `Age`, clears `_instance` when `Age >= Duration`.

**`Apply`:**
```csharp
float t       = instance.Age / instance.Duration;
float shatter = MathF.Sin(t * MathF.PI);   // 0 → 1 → 0

_pOrigin.SetValue(new Vector2(
    instance.Position.X / vp.Width,
    instance.Position.Y / vp.Height));
_pStrength.SetValue(instance.Strength * shatter);
_pRadius.SetValue(instance.Radius);
_pAspectRatio.SetValue((float)vp.Width / vp.Height);
```
Single `spriteBatch.Begin/Draw/End`.

---

## Shader: Distorter_ZoomBlur.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene |
| `Origin` | `float2` | UV-space impact point |
| `Strength` | `float` | Current displacement magnitude (pre-multiplied by shatter curve) |
| `Radius` | `float` | UV-space distance cap (pixels beyond this are unaffected) |
| `AspectRatio` | `float` | Viewport width / height |

**Algorithm:**

```hlsl
float2 uv  = input.TexCoord;
float2 dir = uv - Origin;            // vector from origin to this pixel

// Aspect-correct distance to match screen-circular falloff
float dist = length(float2(dir.x * AspectRatio, dir.y));

// Skip pixels beyond the radius cap
if (dist > Radius) return tex2D(SceneSampler, uv) * input.Color;

// Push pixel outward — magnitude proportional to distance from origin
// (pixels near origin barely move; edge pixels move most)
float2 displacement = dir * dist * Strength;

float2 sampleUV = clamp(uv + displacement, 0.0, 1.0);
return tex2D(SceneSampler, sampleUV) * input.Color;
```

**Technique:** `ZoomBlur`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

```csharp
public ZoomBlurLayer ZoomBlur { get; private set; }

public void TriggerZoomBlur(
    Vector2 position,
    float strength = 0.05f,
    float radius   = 1.0f,
    float duration = 0.4f)
    => ZoomBlur.Trigger(position, strength, radius, duration);
```

Register `ZoomBlur` in `DistortionLayers`.

---

## Example Project

```csharp
// F10: default zoom blur — heavy hit landing
if (keys.IsKeyDown(Keys.F10) && !_prevKeys.IsKeyDown(Keys.F10))
    _screenFX.TriggerZoomBlur(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f));

// F11: strong zoom — super landing
if (keys.IsKeyDown(Keys.F11) && !_prevKeys.IsKeyDown(Keys.F11))
    _screenFX.TriggerZoomBlur(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        strength: 0.10f, radius: 1.0f, duration: 0.5f);

// F12: tight zoom — small hit, off-center
if (keys.IsKeyDown(Keys.F12) && !_prevKeys.IsKeyDown(Keys.F12))
    _screenFX.TriggerZoomBlur(new Vector2(ScreenWidth * 0.35f, ScreenHeight * 0.4f),
        strength: 0.03f, radius: 0.5f, duration: 0.3f);
```

---

## Visual Design Notes

- **Distance-proportional push:** `displacement = dir * dist * Strength` means pixels close to the origin barely move (near-zero `dist`) while edge pixels move most. This creates an organic zoom-out feel rather than a uniform offset.
- **`sin(t * π)` curve computed in C#:** `Strength` is pre-multiplied by the curve before being passed to the shader, keeping the shader stateless. Same pattern as `GlassShatterLayer`.
- **`Radius` cap:** at 1.0 the full viewport is affected. Reducing it (e.g. 0.5) limits the zoom blast to a region around the hit point, which can feel more focused on smaller impacts.
- **No multi-sample blur:** this is a single UV sample — a "zoom shock" rather than a smooth motion blur. If visual fidelity needs to be upgraded later, the shader can be extended to take multiple samples along the displacement ray.
