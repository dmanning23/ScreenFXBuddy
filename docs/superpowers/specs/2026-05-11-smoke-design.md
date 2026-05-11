# Smoke Effect — Design Spec

Date: 2026-05-11

## Overview

Implement `SmokeLayer` — a procedural animated smoke cloud overlay. When triggered, a billowing cloud of smoke expands upward from a pixel-space origin, with configurable color (gray smoke, black smoke, white steam, etc.). The cloud is rendered entirely in HLSL using Fractional Brownian Motion (FBM) noise — no particle system or texture required. Up to 4 simultaneous instances are supported (FBM is more GPU-intensive than distortion effects).

This is a new `IOverlayLayer` following the same pattern as `SpeedLinesLayer`.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/SmokeLayer.cs` | Manages smoke instances, uploads per-frame shader data |
| Create | `ScreenFXBuddy/Content/Overlay_Smoke.fx` | FBM procedural smoke shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `Smoke` property and `TriggerSmoke` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

Note: `SmokeInstance` is a private `record struct` inside `SmokeLayer.cs` — not a separate file.

---

## SmokeInstance

```csharp
private record struct SmokeInstance(
    Vector2 Position,   // pixel-space emitter origin
    Vector4 Color,      // RGBA color of the smoke (pre-converted from XNA Color)
    float Radius,       // max spread radius in UV units
    float Duration,     // total lifetime in seconds
    float Age           // elapsed time in seconds
);
```

`Color` is stored as `Vector4` (converted from `Microsoft.Xna.Framework.Color` at trigger time via `.ToVector4()`) so it can be uploaded directly to the shader's `float4` parameter without per-frame conversion.

---

## SmokeLayer

New class. Implements `IOverlayLayer, IDisposable` (mirrors `SpeedLinesLayer`).

**Constants:**
```csharp
private const int MaxInstances = 4;
```

**Cached EffectParameter handles** (set in `LoadContent`):
- `_pOrigin`      — `float2` (one instance per draw call)
- `_pSmokeColor`  — `float4`
- `_pRadius`      — `float`
- `_pProgress`    — `float` (0→1 over lifetime, drives expansion and fade)
- `_pTime`        — `float` (accumulated global time, drives FBM animation)
- `_pAspectRatio` — `float`

**Design note:** Unlike the distortion layers (which pack all instances into arrays and draw once), `SmokeLayer` issues **one draw call per instance** — identical to `SpeedLinesLayer`. FBM requires more shader math per pixel; packing 4 instances into a single shader pass would require 4× FBM evaluations per pixel simultaneously, which is wasteful. One draw call per instance keeps the shader simple and the hot path lean.

A `float _time` field accumulates `gameTime.ElapsedGameTime.TotalSeconds` each `Update` and is passed to the shader.

A `Texture2D _whitePixel` (1×1 white) is created in `LoadContent` for the `SpriteBatch.Draw` call, same as `SpeedLinesLayer`.

**`LoadContent`:** creates `_whitePixel`, loads `"Overlay_Smoke"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    Color color,
    float radius   = 0.15f,
    float duration = 2.0f)
```
Adds a new `SmokeInstance` if `_instances.Count < MaxInstances`.

**`Update`:** increments `Age` and `_time` each frame, removes instances where `Age >= Duration`.

**`Apply`:** for each active instance:
1. Convert `Position` to UV.
2. Compute `progress = instance.Age / instance.Duration` (clamped 0→1).
3. Set `_pOrigin`, `_pSmokeColor`, `_pRadius`, `_pProgress`, `_pTime`, `_pAspectRatio`.
4. `spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, ..., _effect)`.
5. `spriteBatch.Draw(_whitePixel, viewport.Bounds, Color.White)`.
6. `spriteBatch.End()`.

---

## Shader: Overlay_Smoke.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `Origin` | `float2` | UV-space emitter position |
| `SmokeColor` | `float4` | RGBA smoke tint |
| `Radius` | `float` | Max spread radius (UV units) |
| `Progress` | `float` | 0→1 over lifetime |
| `Time` | `float` | Accumulated time (drives FBM drift) |
| `AspectRatio` | `float` | Viewport width / height |

**Value noise and FBM helpers:**

```hlsl
float valueNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);   // smoothstep

    float a = frac(sin(dot(i,              float2(127.1, 311.7))) * 43758.5453);
    float b = frac(sin(dot(i + float2(1,0),float2(127.1, 311.7))) * 43758.5453);
    float c = frac(sin(dot(i + float2(0,1),float2(127.1, 311.7))) * 43758.5453);
    float d = frac(sin(dot(i + float2(1,1),float2(127.1, 311.7))) * 43758.5453);

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p) {
    float v   = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 5; i++) {
        v   += amp * valueNoise(p);
        p   *= 2.0;
        amp *= 0.5;
    }
    return v;
}
```

**Algorithm:**

```
// Aspect-correct distance from origin
dx = (uv.x - Origin.x) * AspectRatio
dy = uv.y - Origin.y
dist = sqrt(dx*dx + dy*dy)

currentRadius = Radius * Progress   // cloud expands with progress
if dist > currentRadius * 1.5: return transparent   // early out

// Animate: drift upward and add turbulence
driftedUV = float2(
    uv.x + fbm(uv * 3.0 + float2(Time * 0.1, 0)) * 0.05,
    uv.y - Time * 0.04                              // upward drift
)

// FBM cloud density at this pixel
density = fbm((driftedUV - Origin) / (Radius + 0.001) * 2.5 + float2(Time * 0.2, 0))

// Shape: radial envelope (smooth falloff at cloud edge)
radialFade = 1.0 - smoothstep(currentRadius * 0.5, currentRadius, dist)

// Vertical bias: favour upward — suppress below origin
vertBias = saturate((Origin.y - uv.y) / (currentRadius + 0.001) + 0.3)

// Lifetime fade: ramp in over first 20%, ramp out over last 30%
lifeFade = Progress < 0.2 ? Progress / 0.2
         : Progress < 0.7 ? 1.0
         : 1.0 - (Progress - 0.7) / 0.3

alpha = density * radialFade * vertBias * lifeFade * SmokeColor.a * 0.6
return float4(SmokeColor.rgb * alpha, alpha)   // pre-multiplied alpha for additive blend
```

**Technique:** `Smoke`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

Add property and trigger method:

```csharp
public SmokeLayer Smoke { get; private set; }

public void TriggerSmoke(
    Vector2 position,
    Color color,
    float radius   = 0.15f,
    float duration = 2.0f)
    => Smoke.Trigger(position, color, radius, duration);
```

Register `Smoke` in `OverlayLayers` alongside `SpeedLines`.

---

## Example Project

Add keybindings in `Game1.cs`:

```csharp
// N: gray smoke — default
if (keys.IsKeyDown(Keys.N) && !_prevKeys.IsKeyDown(Keys.N))
    _screenFX.TriggerSmoke(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f), Color.Gray);

// M: black smoke — thick explosion
if (keys.IsKeyDown(Keys.M) && !_prevKeys.IsKeyDown(Keys.M))
    _screenFX.TriggerSmoke(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        new Color(30, 30, 30), radius: 0.25f, duration: 3.5f);

// Comma: white steam — fast dissipating
if (keys.IsKeyDown(Keys.OemComma) && !_prevKeys.IsKeyDown(Keys.OemComma))
    _screenFX.TriggerSmoke(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.75f),
        Color.WhiteSmoke, radius: 0.1f, duration: 1.0f);
```

---

## Visual Design Notes

- **FBM density:** 5 octaves of value noise provide enough organic detail without excessive GPU cost. The noise is evaluated in the drifted coordinate space so the cloud appears to billow and shift over time rather than being a static shape that moves.
- **Upward drift:** The sample coordinate is shifted upward (`y -= Time * 0.04`) each frame, which makes the density pattern appear to rise. Combined with the turbulence pass, this avoids the "swimming" look of pure noise.
- **Vertical bias (`vertBias`):** Suppresses smoke below the emitter origin so it doesn't visually "fall". The `+ 0.3` offset allows a little spread downward before cutting off.
- **Additive blend:** Multiple overlapping smoke clouds layer naturally. Light-colored smoke (white/gray) brightens the scene; this is correct for the billowing-cloud look.
- **MaxInstances = 4:** FBM evaluates 5 noise octaves per pixel per draw call. At 4 draw calls the per-frame cost is acceptable; at 8 it would be heavy on integrated GPUs.
