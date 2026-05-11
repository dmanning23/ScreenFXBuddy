# Heat Haze Effect — Design Spec

Date: 2026-05-11

## Overview

Implement the `HeatHazeLayer` distortion effect. A localized column of shimmering heat displacement rises upward from an impact point — like heat coming off fire, an explosion aftermath, or engine exhaust. Up to 8 simultaneous sources are supported. This is a pure distortion effect (warps scene pixels, no visible overlay geometry), following the same architecture as `GravityWaveLayer`.

The existing `HeatHazeLayer.cs` stub uses `Debug_Color` as a placeholder. The stub is entirely replaced by this implementation.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `ScreenFXBuddy/Effects/HeatHazeLayer.cs` | Replace stub with full implementation |
| Create | `ScreenFXBuddy/Content/Distorter_HeatHaze.fx` | Layered-sine heat shimmer shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Update `TriggerHeatHaze` signature |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

Note: `HazeInstance` is a private `record struct` inside `HeatHazeLayer.cs` — not a separate file.

---

## HazeInstance

```csharp
private record struct HazeInstance(
    Vector2 Position,   // pixel-space heat source
    float Strength,     // peak displacement magnitude in UV units
    float Radius,       // horizontal spread (UV units) — half-width of heat column
    float Height,       // how high distortion rises above source (UV units)
    float Duration,     // total lifetime in seconds
    float Age           // elapsed time in seconds
);
```

---

## HeatHazeLayer

Replaces the stub. Implements `IDistortionLayer`.

**Constants:**
```csharp
private const int MaxInstances = 8;
```

**Cached EffectParameter handles** (set in `LoadContent`):
- `_pHazeCount`
- `_pHazeOrigins`  — `float4[MaxInstances]`
- `_pHazeState`    — `float4[MaxInstances]`
- `_pAspectRatio`
- `_pTime`

**Reusable upload buffers** (allocated once):
```csharp
private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];
```

A `float _time` field accumulates `gameTime.ElapsedGameTime.TotalSeconds` each `Update` call and is passed to the shader for animation.

**`LoadContent`:** loads `"Distorter_HeatHaze"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    float strength = 0.02f,
    float radius   = 0.15f,
    float height   = 0.40f,
    float duration = 3.0f)
```
Adds a new `HazeInstance` if `_instances.Count < MaxInstances`.

**`Update`:** increments `Age` each frame (and `_time`), removes instances where `Age >= Duration`.

**`Apply`:**
1. Convert each instance's `Position` to UV by dividing by viewport width/height.
2. Pack into `_originBuffer[i]`: `(originX_uv, originY_uv, 0, 0)`
3. Pack into `_stateBuffer[i]`: `(radius_uv, height_uv, strength * (1f - age/duration), 0)` — strength fades linearly to zero.
4. Set all shader parameters including `_pTime`.
5. Single `spriteBatch.Begin/Draw/End` call.

---

## Shader: Distorter_HeatHaze.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene (bound by SpriteBatch) |
| `HazeCount` | `float` | Number of active instances |
| `HazeOrigins` | `float4[8]` | (originX_uv, originY_uv, 0, 0) per instance |
| `HazeState` | `float4[8]` | (radius_uv, height_uv, strength, 0) per instance |
| `AspectRatio` | `float` | Viewport width / height |
| `Time` | `float` | Accumulated time in seconds (drives animation) |

**Algorithm:**

```
totalDisplace = (0, 0)

for i in 0..HazeCount-1:
  originX  = HazeOrigins[i].x
  originY  = HazeOrigins[i].y
  radius   = HazeState[i].x
  height   = HazeState[i].y
  strength = HazeState[i].z

  dx = uv.x - originX        // signed horizontal offset from column center
  dy = originY - uv.y        // height above source (positive = above)

  if dy < 0 or dy > height: skip
  if abs(dx) > radius: skip   // both in UV units — no aspect correction needed here

  // Lateral envelope: gaussian falloff from column centerline
  lateralFade = exp(-(dx * dx * AspectRatio * AspectRatio) / (radius * radius * 0.5))

  // Vertical envelope: linear from full at source to zero at height
  vertFade = 1.0 - (dy / height)

  // Two-layer sine shimmer (different frequencies and drift speeds)
  wave1 = sin(dy * 12.0 + Time * 2.3) * sin(dy * 7.3 - Time * 1.7)
  wave2 = sin(dy *  8.1 - Time * 3.1) * sin(dy * 5.7 + Time * 2.1) * 0.5

  totalDisplace.x += (wave1 + wave2) * strength * lateralFade * vertFade
  totalDisplace.y += (wave1 * 0.15)  * strength * lateralFade * vertFade

sample SceneSampler at clamp(uv + totalDisplace, 0, 1)
```

**Technique:** `HeatHaze`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

Replace the existing `TriggerHeatHaze` stub method:

```csharp
public void TriggerHeatHaze(
    Vector2 position,
    float strength = 0.02f,
    float radius   = 0.15f,
    float height   = 0.40f,
    float duration = 3.0f)
    => HeatHaze.Trigger(position, strength, radius, height, duration);
```

---

## Example Project

Add keybindings in `Game1.cs`:

```csharp
// H: default heat haze — gentle shimmer
if (keys.IsKeyDown(Keys.H) && !_prevKeys.IsKeyDown(Keys.H))
    _screenFX.TriggerHeatHaze(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f));

// J: intense wide haze — large explosion aftermath
if (keys.IsKeyDown(Keys.J) && !_prevKeys.IsKeyDown(Keys.J))
    _screenFX.TriggerHeatHaze(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.05f, radius: 0.3f, height: 0.7f, duration: 4.0f);

// K: tight strong burst — engine exhaust / small fire
if (keys.IsKeyDown(Keys.K) && !_prevKeys.IsKeyDown(Keys.K))
    _screenFX.TriggerHeatHaze(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        strength: 0.04f, radius: 0.06f, height: 0.25f, duration: 2.0f);
```

---

## Visual Design Notes

- **Shape:** Upward-rising column — only pixels above the source point are affected.
- **Shimmer pattern:** Two layered sine waves at different frequencies and drift speeds produce an organic, non-repeating ripple that avoids the mechanical look of a single sine wave.
- **Lateral containment:** Gaussian falloff from the column centerline keeps the distortion from bleeding into unrelated parts of the scene.
- **Fade:** Displacement strength fades linearly to zero over the lifetime.
- **Time accumulation:** `_time` keeps incrementing across instances — all active hazes share the same clock, which is fine since they animate independently via their spatial position.
