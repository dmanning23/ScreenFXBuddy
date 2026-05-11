# FrostLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `FrostLayer` — a procedural ice crystal overlay that expands radially from a pixel-space origin. FBM noise sampled in a directionally-biased coordinate system creates the needle-and-facet texture of frost crystals. The frost expands outward over the first half of its lifetime and fades over the second half. Configurable tint color (icy blue-white by default, but supports any color for stylized or non-ice uses). Single instance, fire-and-forget.

This is a new `IOverlayLayer, IDisposable`.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/FrostLayer.cs` | Manages single instance, uploads shader params |
| Create | `ScreenFXBuddy/Content/Overlay_Frost.fx` | FBM crystal pattern shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `Frost` property and `TriggerFrost` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

---

## FrostLayer

New class. Implements `IOverlayLayer, IDisposable`. Single instance.

**State fields:**
```csharp
private Vector4 _tintColor;
private float   _radius;
private float   _duration;
private float   _age;
private bool    _active;
```

**Cached EffectParameter handles:**
- `_pOrigin`      — `float2`
- `_pTintColor`   — `float4`
- `_pRadius`      — `float`
- `_pProgress`    — `float` (0→1 over lifetime)
- `_pAspectRatio` — `float`

`public bool IsActive => _active;`

A `Texture2D _whitePixel` is created in `LoadContent`.

**`LoadContent`:** creates `_whitePixel`, loads `"Overlay_Frost"`, caches handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    Color tintColor,
    float radius   = 0.25f,
    float duration = 1.50f)
```
Converts `tintColor` to `Vector4`. Always replaces any active instance.

**`Update`:** increments `_age`, sets `_active = false` when `_age >= _duration`.

**`Apply`:**
```csharp
float progress = MathHelper.Clamp(_age / _duration, 0f, 1f);
// Set parameters and draw
spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, ..., _effect);
spriteBatch.Draw(_whitePixel, viewport.Bounds, Color.White);
spriteBatch.End();
```

---

## Shader: Overlay_Frost.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `Origin` | `float2` | UV-space origin of the frost |
| `TintColor` | `float4` | RGBA frost color (default icy blue-white) |
| `Radius` | `float` | Max spread radius (UV units) |
| `Progress` | `float` | 0→1 over lifetime |
| `AspectRatio` | `float` | Viewport width / height |

**Value noise and FBM helpers** (same as `Overlay_Smoke.fx` and `Overlay_Electric.fx`).

**Algorithm:**

```hlsl
float2 offset = float2((uv.x - Origin.x) * AspectRatio, uv.y - Origin.y);
float dist    = length(offset);

// Frost expands over first half, then holds at full radius
float frostRadius = Radius * min(Progress * 2.0, 1.0);

if (dist > frostRadius * 1.15) return float4(0, 0, 0, 0);

// Crystal coordinate system (polar):
//   Sample FBM using (angle, dist) polar coordinates so that high-density
//   regions naturally align along spokes radiating from the origin.
//   Scaling angle higher creates more ring-facets; scaling dist lower
//   elongates each needle along its spoke.
float angle = atan2(offset.y, offset.x);  // -π to +π

float2 crystalCoord = float2(
    angle * 18.0 / 3.14159,  // many thin facets around the ring
    dist  *  6.0              // elongated needles along each spoke
);

float density = fbm(crystalCoord);

// Threshold for crystal facets — slightly softer than electric
float crystals = smoothstep(0.38, 0.62, density);

// Radial envelope: hard at origin, soft at edge
float radialFade = 1.0 - smoothstep(frostRadius * 0.5, frostRadius, dist);

// Lifetime fade: hold until 60%, then fade out
float lifeFade = Progress < 0.6 ? 1.0 : 1.0 - (Progress - 0.6) / 0.4;

// Sparkle highlight: bright spots at local noise peaks
float sparkle = smoothstep(0.72, 0.85, density) * 0.5;

float alpha = (crystals * 0.6 + sparkle) * radialFade * lifeFade * TintColor.a;
return float4(TintColor.rgb * alpha, alpha);
```

**Technique:** `Frost`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

```csharp
public FrostLayer Frost { get; private set; }

public void TriggerFrost(
    Vector2 position,
    Color tintColor,
    float radius   = 0.25f,
    float duration = 1.50f)
    => Frost.Trigger(position, tintColor, radius, duration);
```

Register `Frost` in `OverlayLayers`.

---

## Example Project

```csharp
// NumPad4: icy blue — cryo special move
if (keys.IsKeyDown(Keys.NumPad4) && !_prevKeys.IsKeyDown(Keys.NumPad4))
    _screenFX.TriggerFrost(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(180, 220, 255));

// NumPad5: deep blue — heavy freeze
if (keys.IsKeyDown(Keys.NumPad5) && !_prevKeys.IsKeyDown(Keys.NumPad5))
    _screenFX.TriggerFrost(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(100, 160, 255), radius: 0.40f, duration: 2.0f);

// NumPad6: white — ice super, almost full screen
if (keys.IsKeyDown(Keys.NumPad6) && !_prevKeys.IsKeyDown(Keys.NumPad6))
    _screenFX.TriggerFrost(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        Color.White, radius: 0.55f, duration: 2.5f);
```

---

## Visual Design Notes

- **Polar crystal coordinates:** sampling FBM in `(angle, dist)` polar space — just like the Electric shader samples in `(angle, dist)` — naturally produces regions that align along radial spokes, giving needles that point outward from the origin. Scaling the angle axis higher (`* 18.0 / π`) creates more ring-facets around the circle; scaling the dist axis lower (`* 6.0` vs Electric's `12.0`) elongates each needle along its spoke. The Frost and Electric shaders share this polar-FBM technique; Frost just uses different scale constants and no Time drift.
- **Expansion over first half:** `frostRadius = Radius * min(Progress * 2.0, 1.0)` causes the frost to reach full size at `Progress = 0.5`, then hold while fading out. This matches how ice looks — it spreads quickly and then lingers.
- **Sparkle layer:** the `smoothstep(0.72, 0.85, density)` pass picks out only the highest-density noise peaks and adds an extra brightness highlight, creating the glittery refraction points visible in real frost.
- **No Time parameter:** unlike `SmokeLayer` and `ElectricLayer`, frost does not animate in place — real frost crystals don't move once formed. The only animation is the radial expansion and fade, driven purely by `Progress`.
- **Pairing with FreezeFrameLayer:** `TriggerFrost` + `TriggerFreezeFrame(new Color(100, 160, 255))` fired simultaneously creates a complete "cryo special lands" moment — the scene desaturates with a cold tint while frost crystals bloom over the hit area.
