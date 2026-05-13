# Speed Lines Effect — Design Spec

Date: 2026-05-01

## Overview

Add a **SpeedLines** overlay effect to ScreenFXBuddy that renders radial burst lines emanating from a given screen point. The visual is the classic anime/manga "speed lines" or "force lines" used in fighting games (KOF, Street Fighter Alpha) during super moves and impacts.

Multiple simultaneous bursts are supported by maintaining a list of `SpeedLinesInstance` objects, matching the `HitFlashLayer` pattern.

---

## New Files

| File | Purpose |
|---|---|
| `ScreenFXBuddy/Effects/SpeedLinesInstance.cs` | Per-burst state: position, color, timers, computed values |
| `ScreenFXBuddy/Effects/SpeedLinesLayer.cs` | `IOverlayLayer` implementation, manages instance list |
| `ScreenFXBuddy/Content/Overlay_SpeedLines.fx` | HLSL shader generating radial lines via angular hash |

## Modified Files

| File | Change |
|---|---|
| `ScreenFXBuddy/ScreenFXComponent.cs` | Add `SpeedLines` property, register in `OverlayLayers`, add `TriggerSpeedLines()` |
| `ScreenFXBuddy.Example/Content/Content.mgcb` | Register `Overlay_SpeedLines.fx` |
| `ScreenFXBuddy.Example/Game1.cs` | Add keybind to test the effect |

---

## New Enum

Defined in `SpeedLinesInstance.cs`:

```csharp
public enum SpeedLinesMode
{
    Static,  // lines appear at full intensity immediately
    Expand   // lines expand outward from center over the lifetime
}
```

`FadeMode` and `FadeCurve` are reused from `HitFlashInstance.cs` unchanged.

---

## SpeedLinesInstance

Mirrors `HitFlashInstance` in structure.

**Constructor:**
```csharp
SpeedLinesInstance(Vector2 pixelPosition, Color color,
    SpeedLinesMode linesMode, FadeMode fadeMode, FadeCurve fadeCurve,
    int lineCount, float maxRadius, float duration)
```

**Per-frame computed values** (consumed by `SpeedLinesLayer.Apply`):

| Property | Description |
|---|---|
| `PixelPosition` | Raw pixel-space origin, converted to UV in `Apply()` |
| `Color` | Line tint |
| `LineCount` | Number of angular segments |
| `MaxRadius` | UV-space outer edge of lines (default 1.0 = full screen) |
| `CurrentAlpha` | Scalar 0–1 derived from `FadeMode` + `FadeCurve` + timer |
| `CurrentInnerRadius` | `0` when `Static`; grows `0 → MaxRadius` linearly over lifetime when `Expand` |
| `IsAlive` | `Timer.HasTimeRemaining` |

The `FadeCurve` logic (logarithmic, exponential, linear) is copied from `HitFlashInstance.ApplyCurve`.

---

## SpeedLinesLayer

Implements `IOverlayLayer`, `IDisposable`.

- Holds `List<SpeedLinesInstance> _instances`
- `LoadContent` loads `Overlay_SpeedLines.fx`, caches `EffectParameter` handles
- `Update` ticks all instances, removes dead ones
- `Apply` loops active instances:
  1. Converts `PixelPosition` to UV by dividing by viewport width/height
  2. Sets shader params (`Center`, `LineColor`, `LineCount`, `InnerRadius`, `MaxRadius`)
  3. `spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, ..., _effect)`
  4. `spriteBatch.Draw(_whitePixel, viewport.Bounds, Color.White)`
  5. `spriteBatch.End()`

One draw call per active instance (same pattern as `HitFlashLayer`). `BlendState.Additive` so lines brighten the scene rather than occlude it.

---

## Shader: Overlay_SpeedLines.fx

**Parameters:**

| Name | Type | Description |
|---|---|---|
| `Center` | `float2` | UV-space origin of the burst |
| `LineColor` | `float4` | RGBA tint including current alpha |
| `LineCount` | `float` | Number of angular line segments |
| `InnerRadius` | `float` | Pixels within this UV radius are transparent (expand cutoff) |
| `MaxRadius` | `float` | Pixels beyond this UV radius fade to zero |

**Algorithm:**

```
For each pixel:
  dir = uv - Center
  dist = length(dir)
  if dist < InnerRadius → discard (transparent)
  if dist > MaxRadius  → discard (transparent)

  angle = atan2(dir.y, dir.x)           // -π to π
  segment = floor(angle / (2π) * LineCount)
  hash = frac(sin(segment * 127.1 + 311.7) * 43758.5453)
  if hash < 0.5 → this angular segment is a line, else gap

  edgeFade = smoothstep(MaxRadius, MaxRadius * 0.7, dist)  // fade near outer edge
  alpha = LineColor.a * edgeFade

output = float4(LineColor.rgb, alpha)
```

The hash threshold (0.5) controls density — roughly half the segments are lines. This is a constant; the visual variety comes from the random hash per angular segment.

---

## ScreenFXComponent Changes

```csharp
public SpeedLinesLayer SpeedLines { get; private set; }

// In LoadContent:
SpeedLines = new SpeedLinesLayer(GraphicsDevice);
OverlayLayers.Add(SpeedLines);
SpeedLines.LoadContent(Game.Content);

// Convenience trigger:
public void TriggerSpeedLines(
    Vector2 position,
    Color color,
    SpeedLinesMode linesMode = SpeedLinesMode.Expand,
    FadeMode fadeMode        = FadeMode.FadeOut,
    FadeCurve fadeCurve      = FadeCurve.Logarithmic,
    int lineCount            = 24,
    float maxRadius          = 1.0f,
    float duration           = 1f)
    => SpeedLines.Trigger(position, color, linesMode, fadeMode, fadeCurve, lineCount, maxRadius, duration);
```

---

## Example Project

- Add `Keys.D8` → `TriggerSpeedLines(screenCenter, Color.White)` for basic test
- Add a few variant bindings to test Static vs Expand and different line counts/colors
