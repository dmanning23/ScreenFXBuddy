# Gravity Wave Effect — Design Spec

Date: 2026-05-02

## Overview

Implement the `GravityWaveLayer` distortion effect. When a player hits the ground, two crescents of energy shoot left and right from the impact point — like the shockwaves of an earthquake. Each crescent is a band of pixel displacement that travels horizontally outward, growing taller as it goes. The push direction follows the crescent's surface normal (outward + upward along the arc), giving an organic pressure-wave feel.

This is a pure distortion effect (warps scene pixels, no visible overlay geometry), identical in architecture to `ForceRippleLayer`.

---

## Existing Stub

`GravityWaveLayer.cs` already exists as an `IDistortionLayer` stub. It uses `Debug_Color` as a placeholder effect and has a `WaveInstance` record struct with `Position`, `Strength`, and `Age` fields. The stub is entirely replaced by this implementation.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|---------------|
| Modify | `ScreenFXBuddy/Effects/GravityWaveLayer.cs` | Replace stub with full implementation |
| Create | `ScreenFXBuddy/Content/Distorter_GravityWave.fx` | Crescent distortion shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Update `TriggerGravityWave` signature |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

Note: `WaveInstance` is a private `record struct` inside `GravityWaveLayer.cs` — it is not a separate file.

---

## WaveInstance

Replace the existing `record struct WaveInstance` with:

```csharp
private record struct WaveInstance(
    Vector2 Position,    // pixel-space impact point
    float Strength,      // peak displacement magnitude in UV units
    float StartHeight,   // crescent height (UV) at t=0
    float EndHeight,     // crescent height (UV) at end of life
    float Speed,         // outward travel speed in UV/sec
    float Duration,      // total lifetime in seconds
    float Age            // elapsed time in seconds
);
```

---

## GravityWaveLayer

Replaces the stub. Implements `IDistortionLayer`.

**Constants:**
```csharp
private const int MaxInstances = 8;
private const float BandWidth  = 0.06f; // UV-space half-width of the distortion band
```

**Cached EffectParameter handles** (set in `LoadContent`):
- `_pWaveCount`
- `_pWaveOrigins` — float4[MaxInstances]
- `_pWaveState`   — float4[MaxInstances]
- `_pAspectRatio`

**Reusable upload buffers** (allocated once, reused each frame):
```csharp
private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];
```

**`LoadContent`:** loads `"Distorter_GravityWave"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    float strength    = 0.04f,
    float startHeight = 0.05f,
    float endHeight   = 0.25f,
    float speed       = 0.5f,
    float duration    = 1.5f)
```
Adds a new `WaveInstance` if `_instances.Count < MaxInstances`.

**`Update`:** increments `Age` each frame, removes instances where `Age >= Duration`.

**`Apply`:**
1. Convert each instance's `PixelPosition` to UV by dividing by viewport width/height.
2. Compute per-instance values:
   - `travelX = instance.Age * instance.Speed` (UV units traveled)
   - `arcH = lerp(instance.StartHeight, instance.EndHeight, instance.Age / instance.Duration)`
   - `strength = instance.Strength * (1f - instance.Age / instance.Duration)` (fade out)
3. Pack into `_originBuffer` and `_stateBuffer`.
4. Set all shader parameters.
5. Single `spriteBatch.Begin/Draw/End` call: draw `source` to `destination`.

---

## Shader: Distorter_GravityWave.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene (bound by SpriteBatch) |
| `WaveCount` | `float` | Number of active instances |
| `WaveOrigins` | `float4[8]` | (originX_uv, originY_uv, 0, 0) per instance |
| `WaveState` | `float4[8]` | (travelX_uv, arcH_uv, strength, 0) per instance |
| `AspectRatio` | `float` | Viewport width / height |
| `BandWidth` | `float` | UV half-width of distortion band (set from C# constant) |

**Algorithm:**

```
totalDisplace = (0, 0)

for i in 0..WaveCount-1:
  originX = WaveOrigins[i].x
  originY = WaveOrigins[i].y
  travelX  = WaveState[i].x
  arcH     = WaveState[i].y
  strength = WaveState[i].z

  for side in (-1, +1):   // left crescent and right crescent
    waveX = originX + side * travelX

    dx = uv.x - waveX          // signed horizontal distance from wave front
    dy = originY - uv.y        // height above ground (positive = above)

    if |dx| > BandWidth: skip
    if dy < 0 or dy > arcH: skip

    // Smooth falloffs
    hFade = exp(-(dx*dx) / (BandWidth*BandWidth * 0.3))   // gaussian across band
    vFade = sin(dy / arcH * PI)                            // 0 at ground, peaks at mid-arc, 0 at top

    // Crescent surface normal in screen space (outward + upward)
    nx = side * AspectRatio       // outward, aspect-corrected so normal is screen-circular
    ny = -(dy / arcH)             // upward component (0 at ground, -1 at arc peak)
    len = sqrt(nx*nx + ny*ny)
    nx /= len; ny /= len

    // Convert normal back to UV space for displacement
    totalDisplace.x += nx / AspectRatio * strength * hFade * vFade
    totalDisplace.y += ny             * strength * hFade * vFade

sample SceneSampler at (uv + totalDisplace)
```

**Technique:** `DistortionGravityWave`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

Update the existing `TriggerGravityWave` method signature:

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

The existing `TriggerGravityWave(Vector2 position, float strength = 1f)` call in `Game1.cs` will break — update it to use the new defaults.

---

## Example Project

Replace the existing D2 binding and add two variant bindings in `Game1.cs`:

```csharp
// D2: default gravity wave — REPLACE the existing D2 binding (old signature is broken)
if (keys.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2))
    _screenFX.TriggerGravityWave(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f));

// I: slow wide wave
if (keys.IsKeyDown(Keys.I) && !_prevKeys.IsKeyDown(Keys.I))
    _screenFX.TriggerGravityWave(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.06f, startHeight: 0.02f, endHeight: 0.4f, speed: 0.3f, duration: 2.5f);

// O: fast tight wave
if (keys.IsKeyDown(Keys.O) && !_prevKeys.IsKeyDown(Keys.O))
    _screenFX.TriggerGravityWave(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.03f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.9f, duration: 0.8f);
```

---

## Visual Design Decisions

- **Shape:** Expanding crescent — crescent height grows from `startHeight` to `endHeight` over the lifetime.
- **Push direction:** Crescent surface normal (outward + upward along arc) — feels like a real pressure wave.
- **Band profile:** Gaussian falloff horizontally across the wave band, sine curve vertically along the crescent height. This gives a smooth leading and trailing edge.
- **Fade:** Displacement strength fades linearly to zero over the lifetime.
- **Two crescents:** Always generated symmetrically left and right from the impact point in the shader — not separate instances.
