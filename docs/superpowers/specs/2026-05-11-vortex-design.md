# VortexLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `VortexLayer` — a swirling pixel distortion that rotates the scene around a pixel-space origin. Pixels close to the origin rotate sharply (fast inner swirl); pixels far from it barely move (the distortion tapers off with distance). The effect is perfect for air dashes, teleports, wind-based specials, or any move involving circular energy. Up to 4 simultaneous instances, packed `float4[]` arrays, `IDistortionLayer` — same architecture as `GravityWaveLayer`.

`speed` is signed: positive = clockwise, negative = counter-clockwise.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/VortexLayer.cs` | Manages instances, uploads packed shader arrays |
| Create | `ScreenFXBuddy/Content/Distorter_Vortex.fx` | Inverse-distance UV rotation shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `Vortex` property and `TriggerVortex` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

Note: `VortexInstance` is a private `record struct` inside `VortexLayer.cs`.

---

## VortexInstance

```csharp
private record struct VortexInstance(
    Vector2 Position,   // pixel-space swirl center
    float Strength,     // peak swirl magnitude (radians * UV-dist at unit distance)
    float Radius,       // UV-space outer edge — pixels beyond are unaffected
    float Speed,        // signed: positive = clockwise, negative = counter-clockwise
    float Duration,
    float Age
);
```

---

## VortexLayer

New class. Implements `IDistortionLayer`.

**Constants:**
```csharp
private const int MaxInstances = 4;
```

**Cached EffectParameter handles:**
- `_pVortexCount`   — `float`
- `_pVortexOrigins` — `float4[MaxInstances]`
- `_pVortexState`   — `float4[MaxInstances]`
- `_pAspectRatio`   — `float`

**Reusable upload buffers:**
```csharp
private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];
```

`public bool IsActive => _instances.Count > 0;`

**`LoadContent`:** loads `"Distorter_Vortex"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    float strength = 0.30f,
    float radius   = 0.25f,
    float speed    = 2.00f,
    float duration = 0.60f)
```
Adds a new `VortexInstance` if `_instances.Count < MaxInstances`.

**`Update`:** increments `Age` each frame, removes instances where `Age >= Duration`.

**`Apply`:**

For each instance:
- `t = Age / Duration`
- `swirl = Strength * Speed * (1f - t)` — fades linearly to zero; `Speed` sign controls direction
- Pack `_originBuffer[i]`: `(originX_uv, originY_uv, 0, 0)`
- Pack `_stateBuffer[i]`: `(swirl, radius, 0, 0)`

Set all shader parameters, then single `spriteBatch.Begin/Draw/End`.

---

## Shader: Distorter_Vortex.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene |
| `VortexCount` | `float` | Number of active instances |
| `VortexOrigins` | `float4[4]` | (originX_uv, originY_uv, 0, 0) per instance |
| `VortexState` | `float4[4]` | (swirl, radius, 0, 0) per instance |
| `AspectRatio` | `float` | Viewport width / height |

**Algorithm:**

```hlsl
float2 totalDisplacement = float2(0.0, 0.0);
int count = (int)VortexCount;

for (int i = 0; i < count; i++)
{
    float originX = VortexOrigins[i].x;
    float originY = VortexOrigins[i].y;
    float swirl   = VortexState[i].x;   // signed, pre-faded by C#
    float radius  = VortexState[i].y;

    if (abs(swirl) < 0.0001) continue;

    float2 offset = uv - float2(originX, originY);

    // Aspect-correct distance for circular vortex shape
    float dist = length(float2(offset.x * AspectRatio, offset.y));

    if (dist > radius || dist < 0.001) continue;

    // Radial envelope: full swirl inside radius*0.6, tapers off toward edge
    float radialFade = 1.0 - smoothstep(radius * 0.6, radius, dist);

    // Swirl angle: inversely proportional to distance (inner pixels rotate more)
    // Clamped to avoid extreme near-origin rotation
    float swirlAngle = swirl / max(dist, 0.04) * radialFade;

    // Rotate offset in aspect-corrected space
    float cosA = cos(swirlAngle);
    float sinA = sin(swirlAngle);
    float2 aspectOffset = float2(offset.x * AspectRatio, offset.y);
    float2 rotated = float2(
        aspectOffset.x * cosA - aspectOffset.y * sinA,
        aspectOffset.x * sinA + aspectOffset.y * cosA
    );

    // Convert back from aspect-corrected space and compute displacement
    float2 rotatedUV  = float2(rotated.x / AspectRatio, rotated.y) + float2(originX, originY);
    totalDisplacement += rotatedUV - uv;
}

float2 sampleUV = clamp(uv + totalDisplacement, 0.0, 1.0);
return tex2D(SceneSampler, sampleUV) * input.Color;
```

**Technique:** `Vortex`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

```csharp
public VortexLayer Vortex { get; private set; }

public void TriggerVortex(
    Vector2 position,
    float strength = 0.30f,
    float radius   = 0.25f,
    float speed    = 2.00f,
    float duration = 0.60f)
    => Vortex.Trigger(position, strength, radius, speed, duration);
```

Register `Vortex` in `DistortionLayers`.

---

## Example Project

```csharp
// NumPad7: default clockwise vortex — air dash
if (keys.IsKeyDown(Keys.NumPad7) && !_prevKeys.IsKeyDown(Keys.NumPad7))
    _screenFX.TriggerVortex(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f));

// NumPad8: counter-clockwise — teleport arrival
if (keys.IsKeyDown(Keys.NumPad8) && !_prevKeys.IsKeyDown(Keys.NumPad8))
    _screenFX.TriggerVortex(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        strength: 0.5f, radius: 0.35f, speed: -3.0f, duration: 0.5f);

// NumPad9: fast tight vortex — wind projectile
if (keys.IsKeyDown(Keys.NumPad9) && !_prevKeys.IsKeyDown(Keys.NumPad9))
    _screenFX.TriggerVortex(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        strength: 0.20f, radius: 0.15f, speed: 4.0f, duration: 0.35f);
```

---

## Visual Design Notes

- **Inverse-distance rotation:** `swirlAngle = swirl / max(dist, 0.04)` means a pixel 0.05 UV units from the origin rotates by `swirl / 0.05 = 20 * swirl` radians, while a pixel 0.2 UV units away rotates by only `5 * swirl` radians. This creates the characteristic tight-center / loose-edge vortex shape.
- **`max(dist, 0.04)` clamp:** prevents extreme rotation for pixels very close to the origin (which would cause visual artifacts). Pixels within 0.04 UV units of the origin all receive the same maximum rotation.
- **Aspect-ratio correction:** the rotation is performed in aspect-corrected space (`offset.x * AspectRatio`) so the vortex appears circular on any viewport — without this, it would be elliptical on 16:9 screens.
- **Signed speed:** `speed = 2.0` (clockwise) vs `speed = -2.0` (counter-clockwise). The sign is baked into `swirl` in C# before upload, so the shader itself just uses the sign of `swirl` naturally.
- **Linear fade:** `swirl = Strength * Speed * (1f - t)` fades the swirl to zero cleanly. A `sin(t * π)` curve (like GlassShatter/ZoomBlur) would produce a push-and-return motion; the linear fade was chosen so the vortex spins up at full intensity and gradually unwinds — matching the "a spinning move happened here" feel rather than a shockwave.
- **Stacking:** two vortices at the same point add their displacements. A clockwise + counter-clockwise pair at the same origin cancel out — an intentional design behavior that can be used for visual effects like a "reversal" moment.
