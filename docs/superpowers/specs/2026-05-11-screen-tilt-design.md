# ScreenTiltLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `ScreenTiltLayer` — a brief rotational snap of the entire scene around the screen center. When a heavy hit lands, the screen tilts sharply to one side and eases back, giving a "camera recoil" feel that complements the translation-based `ScreenShakeLayer`. The effect is purely rotational — no translation, no zoom.

This is a new `IDistortionLayer` with its own shader. Single instance.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/ScreenTiltLayer.cs` | Manages single instance, uploads angle each frame |
| Create | `ScreenFXBuddy/Content/Distorter_ScreenTilt.fx` | UV rotation shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `ScreenTilt` property and `TriggerScreenTilt` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

---

## ScreenTiltInstance

```csharp
private record struct ScreenTiltInstance(
    float MaxAngle,    // peak rotation in degrees (positive = clockwise)
    float Duration,    // total lifetime in seconds
    float Age          // elapsed time in seconds
);
```

---

## ScreenTiltLayer

New class. Implements `IDistortionLayer`.

**Single instance:**
```csharp
private ScreenTiltInstance? _instance;
```

**Cached EffectParameter handles:**
- `_pAngle`       — `float` (radians, computed from degrees in `Apply`)
- `_pAspectRatio` — `float`

`public bool IsActive => _instance.HasValue;`

**`LoadContent`:** loads `"Distorter_ScreenTilt"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(float angle = 3.0f, float duration = 0.4f)
```
`angle` is in degrees. Positive = clockwise tilt. Always replaces any in-progress instance.

**`Update`:** increments `Age`, clears `_instance` when `Age >= Duration`.

**`Apply`:**

Compute the current angle using a snap-and-ease-back curve:

```csharp
float t = instance.Age / instance.Duration;

float currentAngle;
const float SnapFraction = 0.1f;   // first 10% of duration = snap to peak
if (t < SnapFraction)
    // Snap in: linear ramp from 0 → maxAngle
    currentAngle = instance.MaxAngle * (t / SnapFraction);
else
{
    // Ease back: quadratic ease from maxAngle → 0
    float easeT = (t - SnapFraction) / (1f - SnapFraction);   // 0→1 over remaining time
    currentAngle = instance.MaxAngle * (1f - easeT * easeT);
}

float angleRadians = MathHelper.ToRadians(currentAngle);
_pAngle.SetValue(angleRadians);
_pAspectRatio.SetValue((float)vp.Width / vp.Height);
```

Single `spriteBatch.Begin/Draw/End`.

---

## Shader: Distorter_ScreenTilt.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene |
| `Angle` | `float` | Current rotation in radians (positive = clockwise) |
| `AspectRatio` | `float` | Viewport width / height |

**Algorithm:**

Rotate UV coordinates around the screen center. Aspect-ratio correction ensures the rotation is circular (not elliptical) in screen space:

```hlsl
float2 uv      = input.TexCoord;
float2 center  = float2(0.5, 0.5);
float2 offset  = uv - center;

// Scale to screen space so rotation is circular
offset.x *= AspectRatio;

// 2D rotation matrix
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
```

**Technique:** `ScreenTilt`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

```csharp
public ScreenTiltLayer ScreenTilt { get; private set; }

public void TriggerScreenTilt(float angle = 3.0f, float duration = 0.4f)
    => ScreenTilt.Trigger(angle, duration);
```

Register `ScreenTilt` in `DistortionLayers`.

---

## Example Project

```csharp
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

---

## Visual Design Notes

- **Snap-and-ease-back curve:** the 10%/90% split gives a sharp physical "recoil" feel — the camera wrenches to the side and recovers. A symmetric `sin(t * π)` curve (as used in ZoomBlur/GlassShatter) would feel too smooth and floaty for a hit reaction.
- **Aspect-ratio correction:** without it, a square viewport would rotate correctly but on a 16:9 screen the rotation would appear elliptical (horizontal lines would arc more than vertical ones). The `offset.x *= AspectRatio` / `rotated.x /= AspectRatio` round-trip fixes this.
- **Clamped edge pixels:** `clamp(sampleUV, 0, 1)` holds edge pixels at the scene boundary during rotation rather than going black. For small angles (2–5°) this is barely visible; for large angles (10°+) the clamping creates a smear at the screen edges which looks acceptable for a dramatic hit.
- **Pairing with ScreenShake:** `TriggerScreenTilt` and `TriggerScreenShake` are independent layers and can be triggered simultaneously. A common fighting game combination for a super landing: `TriggerScreenShake` (translation) + `TriggerScreenTilt` (rotation) + `TriggerZoomBlur` (radial push) all fired at the same time.
- **Angle range:** 2–4° is subtle but readable. 5–8° is dramatic. Beyond 10° the edge clamping becomes noticeable and the effect reads as "broken" rather than "impactful" — not recommended.
