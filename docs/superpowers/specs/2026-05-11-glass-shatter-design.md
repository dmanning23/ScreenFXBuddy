# Glass Shatter Effect — Design Spec

Date: 2026-05-11

## Overview

Implement `GlassShatterLayer` — a procedural Voronoi glass-shattering distortion effect. When triggered, the screen cracks into shards that push outward from the impact point and return — a push-and-return shockwave that momentarily makes the scene look like shattered glass. The Voronoi cell pattern is generated procedurally in the shader from a random seed, so each trigger produces a different crack layout. Crack lines between cells are drawn as a bright overlay on top of the distorted scene.

This is a single-instance `IDistortionLayer` (one active shatter at a time).

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/GlassShatterLayer.cs` | Manages single instance, uploads shader params |
| Create | `ScreenFXBuddy/Content/Distorter_GlassShatter.fx` | Procedural Voronoi distortion + crack line shader |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `GlassShatter` property and `TriggerGlassShatter` method |
| Modify | `ScreenFXBuddy.Example/Content/Content.mgcb` | Register shader |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

Note: `ShatterInstance` is a private `record struct` inside `GlassShatterLayer.cs`.

---

## ShatterInstance

```csharp
private record struct ShatterInstance(
    Vector2 Position,   // pixel-space impact point
    float Strength,     // peak displacement magnitude in UV units
    int NumCells,       // number of Voronoi sites (shards)
    float Seed,         // random float — unique crack pattern per trigger
    float Duration,     // total lifetime in seconds
    float Age           // elapsed time in seconds
);
```

`Seed` is generated at trigger time: `Random.Shared.NextSingle() * 1000f`.

---

## GlassShatterLayer

New class. Implements `IDistortionLayer`.

**Single instance tracking** — no `List<>` or packed arrays. Nullable field:
```csharp
private ShatterInstance? _instance;
```

**Cached EffectParameter handles** (set in `LoadContent`):
- `_pOrigin`     — `float2` (UV-space impact point)
- `_pStrength`   — `float`
- `_pNumCells`   — `float` (passed as float to avoid int-uniform driver quirks)
- `_pSeed`       — `float`
- `_pShatter`    — `float` (0→1→0 animation curve value)
- `_pAspectRatio`— `float`

**`LoadContent`:** loads `"Distorter_GlassShatter"`, caches parameter handles.

**`Trigger`:**
```csharp
public void Trigger(
    Vector2 position,
    float strength = 0.04f,
    int numCells   = 20,
    float duration = 0.8f)
```
Always replaces the current instance (even if one is active) — if a second hit comes in before the first finishes, the new hit takes over.

**`Update`:** increments `Age`, clears `_instance` when `Age >= Duration`.

**`Apply`:**
1. Compute `t = Age / Duration`.
2. Compute `shatter = sin(t * π)` — value rises from 0 at start, peaks at 0.5 duration, returns to 0 at end. This is the push-and-return curve.
3. Convert `Position` to UV.
4. Upload all parameters.
5. Single `spriteBatch.Begin/Draw/End`.

---

## Shader: Distorter_GlassShatter.fx

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| `SceneTexture` | `Texture2D` | Source scene (bound by SpriteBatch) |
| `Origin` | `float2` | UV-space impact point |
| `Strength` | `float` | Peak displacement magnitude (UV units) |
| `NumCells` | `float` | Number of Voronoi sites |
| `Seed` | `float` | Per-trigger random seed |
| `Shatter` | `float` | Animation value 0→1→0 (sin curve) |
| `AspectRatio` | `float` | Viewport width / height |

**Voronoi site generation helper:**

Each of the `NumCells` sites is generated procedurally from its index and `Seed`. No texture or CPU-side data needed:

```hlsl
float2 site(float index) {
    float2 p;
    p.x = frac(sin(index * 127.1 + Seed)          * 43758.5453);
    p.y = frac(sin(index * 311.7 + Seed + 100.0)  * 43758.5453);
    return p;   // in [0,1]² UV space
}
```

**Algorithm:**

```
// Step 1: Find nearest and second-nearest Voronoi sites
minDist1 = 1e9;  minDist2 = 1e9;
nearestIdx = 0;  nearestPos = (0,0);

for k in 0..NumCells-1:
  s = site(k)
  // aspect-correct distance
  d = length(float2((uv.x - s.x) * AspectRatio, uv.y - s.y))
  if d < minDist1:
    minDist2 = minDist1
    minDist1 = d
    nearestIdx = k
    nearestPos = s
  else if d < minDist2:
    minDist2 = d

// Step 2: Compute displacement for this cell
// Direction: away from impact point, in UV space
cellToImpact = nearestPos - Origin
if length(cellToImpact) < 0.001: cellToImpact = float2(1, 0)
dispDir = normalize(cellToImpact)   // pointing away from Origin

// Radial falloff: cells close to impact move more
impactDist = length(float2((nearestPos.x - Origin.x) * AspectRatio, nearestPos.y - Origin.y))
falloff = 1.0 / (1.0 + impactDist * 4.0)

displacement = dispDir * Strength * Shatter * falloff

// Step 3: Sample scene at displaced UV
sampleUV = clamp(uv + displacement, 0, 1)
sceneColor = tex2D(SceneSampler, sampleUV)

// Step 4: Crack lines — bright white/blue where cell boundaries are close
crackWidth = 0.006
boundary = minDist2 - minDist1   // near 0 at cell edges
crackAlpha = Shatter * (1.0 - smoothstep(0, crackWidth, boundary))
crackColor = float4(0.85, 0.95, 1.0, crackAlpha)   // cool blue-white

// Composite: scene with cracks overlaid
return lerp(sceneColor, crackColor, crackAlpha) * input.Color
```

**Technique:** `GlassShatter`, single pass, PixelShader only.

**Performance note:** The `for` loop over `NumCells` runs per pixel. At `NumCells = 20` this is 20 distance calculations per pixel — acceptable on SM3. Exposing `NumCells` as a parameter lets callers reduce it for performance-sensitive contexts. Maximum meaningful value is ~40; above that the shards are too small to read visually.

---

## ScreenFXComponent Changes

Add property and trigger method:

```csharp
public GlassShatterLayer GlassShatter { get; private set; }

public void TriggerGlassShatter(
    Vector2 position,
    float strength = 0.04f,
    int numCells   = 20,
    float duration = 0.8f)
    => GlassShatter.Trigger(position, strength, numCells, duration);
```

Register `GlassShatter` in `DistortionLayers`.

---

## Example Project

Add keybindings in `Game1.cs`:

```csharp
// G: default glass shatter
if (keys.IsKeyDown(Keys.G) && !_prevKeys.IsKeyDown(Keys.G))
    _screenFX.TriggerGlassShatter(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f));

// T (repurpose if free, otherwise pick adjacent): many fine shards
if (keys.IsKeyDown(Keys.OemOpenBrackets) && !_prevKeys.IsKeyDown(Keys.OemOpenBrackets))
    _screenFX.TriggerGlassShatter(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        strength: 0.03f, numCells: 35, duration: 0.6f);

// ]: few large shards — slow dramatic break
if (keys.IsKeyDown(Keys.OemCloseBrackets) && !_prevKeys.IsKeyDown(Keys.OemCloseBrackets))
    _screenFX.TriggerGlassShatter(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        strength: 0.07f, numCells: 8, duration: 1.2f);
```

Check `Game1.cs` before implementing to confirm `[` and `]` keys are not already bound; substitute if needed.

---

## Visual Design Notes

- **Push-and-return curve:** `sin(t * π)` starts at 0, peaks at the midpoint, and returns cleanly to 0 at expiry. The screen returns to its undistorted state with no pop or jump.
- **Procedural sites:** All crack geometry is derived from `Seed` and `NumCells` in the shader — no CPU-side Voronoi computation, no texture uploads. Different seeds produce entirely different crack patterns.
- **Crack line color:** Blue-white (`float4(0.85, 0.95, 1.0, ...)`) reads as glass or ice. The alpha is modulated by `Shatter` so cracks appear and disappear with the animation.
- **Single instance:** Triggering during an active shatter replaces it immediately. This avoids screen-covering overlap issues that would occur with multiple simultaneous full-screen Voronoi effects.
- **Radial falloff:** Cells near the impact point displace most; distant cells barely move. This focuses the visual distortion around the hit and makes the effect read clearly even on busy scenes.
