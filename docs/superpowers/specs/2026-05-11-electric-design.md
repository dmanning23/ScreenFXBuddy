# ElectricLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `ElectricLayer` — a procedural arcing electricity overlay that radiates tendrils from a pixel-space origin. FBM noise sampled in polar coordinates creates the characteristic branching spike pattern of electricity. Up to 4 simultaneous instances (one draw call per instance, additive blend). Configurable color — blue-white, purple, gold, etc.

This is a new `IOverlayLayer, IDisposable` following the same pattern as `SmokeLayer`.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/ElectricLayer.cs` | Manages instances, per-instance draw calls |
| Create | `ScreenFXBuddy/Content/Overlay_Electric.fx` | FBM polar-noise electricity shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `Electric` property and `TriggerElectric` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

Note: `ElectricInstance` is a private `record struct` inside `ElectricLayer.cs`.

---

## ElectricInstance

```csharp
private record struct ElectricInstance(
    Vector2 Position,   // pixel-space origin
    Vector4 Color,      // RGBA pre-converted via .ToVector4()
    float Radius,       // max spread radius in UV units
    float Duration,
    float Age
);
```

---

## ElectricLayer

New class. Implements `IOverlayLayer, IDisposable`.

**Constants:**
```csharp
private const int MaxInstances = 4;
```

**Cached EffectParameter handles:**
- `_pOrigin`      — `float2`
- `_pElecColor`   — `float4`
- `_pRadius`      — `float`
- `_pProgress`    — `float` (0→1 over lifetime)
- `_pTime`        — `float` (accumulated global time, drives FBM animation)
- `_pAspectRatio` — `float`

One draw call per instance (same as `SmokeLayer`). `_time` accumulates in `Update`.

A `Texture2D _whitePixel` (1×1 white) is created in `LoadContent`.

**`LoadContent`:** creates `_whitePixel`, loads `"Overlay_Electric"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    Color color,
    float radius   = 0.20f,
    float duration = 0.50f)
```
Adds a new `ElectricInstance` if `_instances.Count < MaxInstances`.

**`Update`:** increments `Age` and `_time`, removes instances where `Age >= Duration`.

**`Apply`:** for each active instance:
1. Convert `Position` to UV.
2. Compute `progress = Age / Duration`.
3. Set all parameters, draw full viewport with additive blend.

---

## Shader: Overlay_Electric.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `Origin` | `float2` | UV-space emitter position |
| `ElecColor` | `float4` | RGBA electricity tint |
| `Radius` | `float` | Max spread radius (UV units) |
| `Progress` | `float` | 0→1 over lifetime |
| `Time` | `float` | Accumulated time (drives flicker) |
| `AspectRatio` | `float` | Viewport width / height |

**Value noise and FBM helpers** (identical to `Overlay_Smoke.fx`):

```hlsl
float valueNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = frac(sin(dot(i,               float2(127.1, 311.7))) * 43758.5453);
    float b = frac(sin(dot(i + float2(1,0), float2(127.1, 311.7))) * 43758.5453);
    float c = frac(sin(dot(i + float2(0,1), float2(127.1, 311.7))) * 43758.5453);
    float d = frac(sin(dot(i + float2(1,1), float2(127.1, 311.7))) * 43758.5453);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float fbm(float2 p) {
    float v = 0.0, amp = 0.5;
    for (int i = 0; i < 5; i++) { v += amp * valueNoise(p); p *= 2.0; amp *= 0.5; }
    return v;
}
```

**Algorithm:**

```hlsl
float2 offset = float2((uv.x - Origin.x) * AspectRatio, uv.y - Origin.y);
float dist    = length(offset);

if (dist > Radius * 1.1) return float4(0, 0, 0, 0);  // early out

// Polar coordinates (angle around origin)
float angle = atan2(offset.y, offset.x);   // -π to +π

// Sample FBM in polar-stretched space:
//   - angle axis stretched to create angular spikes
//   - dist axis compressed to stack tendrils radially
//   - Time drift animates the flicker/crawl
float2 noiseCoord = float2(
    angle * 4.0 / 3.14159 + Time * 3.0,  // 4 spokes, fast flicker
    dist * 12.0 - Time * 1.5             // inward crawl
);
float density = fbm(noiseCoord);

// Threshold: smoothstep creates discrete bright tendrils with soft edges
float tendrils = smoothstep(0.42, 0.62, density);

// Radial envelope: fade out toward the radius edge
float radialFade = 1.0 - smoothstep(Radius * 0.4, Radius, dist);

// Lifetime fade: ramp in over 20%, hold, ramp out over 30%
float lifeFade = Progress < 0.2 ? Progress / 0.2
               : Progress < 0.7 ? 1.0
               : 1.0 - (Progress - 0.7) / 0.3;

float alpha = tendrils * radialFade * lifeFade * ElecColor.a;

// Slightly overbright core for a glowing-arc feel
float brightness = 1.0 + (1.0 - dist / Radius) * 0.8;
return float4(ElecColor.rgb * alpha * brightness, alpha);
```

**Technique:** `Electric`, single pass, PixelShader only.

---

## ScreenFXComponent Changes

```csharp
public ElectricLayer Electric { get; private set; }

public void TriggerElectric(
    Vector2 position,
    Color color,
    float radius   = 0.20f,
    float duration = 0.50f)
    => Electric.Trigger(position, color, radius, duration);
```

Register `Electric` in `OverlayLayers`.

---

## Example Project

```csharp
// NumPad1: blue-white electric — default shock
if (keys.IsKeyDown(Keys.NumPad1) && !_prevKeys.IsKeyDown(Keys.NumPad1))
    _screenFX.TriggerElectric(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(150, 200, 255));

// NumPad2: purple electricity — dark/shadow character
if (keys.IsKeyDown(Keys.NumPad2) && !_prevKeys.IsKeyDown(Keys.NumPad2))
    _screenFX.TriggerElectric(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(180, 80, 255), radius: 0.25f, duration: 0.7f);

// NumPad3: golden lightning — ki energy
if (keys.IsKeyDown(Keys.NumPad3) && !_prevKeys.IsKeyDown(Keys.NumPad3))
    _screenFX.TriggerElectric(new Vector2(ScreenWidth / 2f, ScreenHeight * 0.4f),
        new Color(255, 200, 60), radius: 0.15f, duration: 0.4f);
```

---

## Visual Design Notes

- **Polar FBM sampling:** sampling FBM along the `(angle, dist)` axis naturally produces radial spikes — high-density regions align along spokes from the origin, matching the look of electric arcs. The `Time` drift makes them flicker and crawl inward, adding life.
- **Threshold smoothstep(0.42, 0.62):** the narrow band produces crisp tendrils with soft edges. Widening it (e.g. 0.3–0.7) creates a more diffuse glow; narrowing it (0.48–0.55) creates sharper, sparser bolts.
- **Overbright core:** multiplying by `1.0 + (1 - dist/Radius) * 0.8` brightens pixels close to the origin, creating the sense of a hot central spark that branches outward.
- **Additive blend:** multiple overlapping electric instances layer naturally and brighten each other — two simultaneous shocks at the same point look like a more intense discharge.
