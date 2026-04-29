# ScreenFXBuddy

A MonoGame library of post-processing screen effects built for fighting games. Drop it in, trigger effects from game code, and let the component handle the render pipeline.

> **Status:** Early development — effects are not yet implemented.

## Effects

| Effect | Description |
|---|---|
| **ForceRipple** | Shockwave distortion radiating from an impact point |
| **GravityWave** | Concentric wave distortion for heavy or super moves |
| **ScreenShake** | Trauma-based camera shake with configurable decay |
| **ChromaticAberration** | RGB channel split, intensity-driven |
| **HeatHaze** | Shimmering heat distortion overlay |
| **HitFlash** | Additive full-screen flash on hit confirmation |
| **AnimeSuper** | Stylized full-screen flash for super/ultimate moves |

Multiple instances of distortion effects (ForceRipple, GravityWave) can be active simultaneously — the renderer batches them into a single shader pass per type.

## Requirements

- .NET 8
- MonoGame 3.8 (DesktopGL)

## Usage

Add the component in your `Game` constructor:

```csharp
var screenFX = new ScreenFXComponent(this);
Components.Add(screenFX);
```

Trigger effects from anywhere in game code:

```csharp
screenFX.TriggerForceRipple(hitPosition);
screenFX.TriggerScreenShake(trauma: 0.8f);
screenFX.TriggerHitFlash(Color.White);
```

The component hooks into the draw pipeline automatically — no changes to your `Draw` method needed.

## How It Works

Each frame, the scene is drawn to an offscreen render target. Active distortion effects are then applied in sequence using a ping-pong render target chain (one GPU pass per effect type). Additive overlay effects (HitFlash, AnimeSuper) are composited on top without reading scene pixels.

## License

MIT
