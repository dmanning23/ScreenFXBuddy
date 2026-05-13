# Effect Layer Plumbing Design

**Date:** 2026-04-28
**Status:** Approved

## Overview

Add the C# plumbing that connects `ScreenFXComponent` to all seven screen effects via a public, extensible layer system. The pipeline uses the Per-Type Ping-Pong architecture (Option C from the initial brainstorm): one GPU pass per effect type, with distortion effects chained through ping-pong render targets and additive overlays composited on top of the back buffer.

## Interfaces

Two interfaces serve the two fundamentally different effect categories:

```csharp
// Reads from source, writes distortion to destination (ping-pong chain)
public interface IDistortionLayer
{
    bool IsActive { get; }
    void Update(GameTime gameTime);
    void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination);
}

// Draws additively on top of the back buffer — never reads scene pixels
public interface IOverlayLayer
{
    bool IsActive { get; }
    void Update(GameTime gameTime);
    void Apply(SpriteBatch spriteBatch);
}
```

`IsActive` lets the pipeline skip layers with nothing to contribute, keeping idle frames cheap.

## Built-in Layers

### Distortion Layers (ping-pong chain, applied in this order)

| Class | Trigger | Instance Data |
|---|---|---|
| `ForceRippleLayer` | `Trigger(Vector2 position, float strength = 1f)` | position, age, strength — up to 16 simultaneous |
| `GravityWaveLayer` | `Trigger(Vector2 position, float strength = 1f)` | position, age, strength — up to 8 simultaneous |
| `ScreenShakeLayer` | `Trigger(float trauma)` | single trauma float, decays per-frame |
| `ChromaticAberrationLayer` | `Trigger(float intensity, float duration)` | single intensity float, decays over duration |
| `HeatHazeLayer` | `Trigger(float intensity, float duration)` | single intensity float, decays over duration |

All multi-instance effects follow the `Distorter_Ripple.fx` recipe: `float4 InstanceData[MAX]`, `float InstanceCount` (as float to avoid driver quirks), loop in the pixel shader.

`ScreenShakeLayer` holds a single trauma float and a random seed that changes each frame; its shader applies a global UV offset scaled by trauma.

`ChromaticAberrationLayer` and `HeatHazeLayer` are triggered with decay (not always-on) — the trigger pattern allows them to be used as hit-stun or impact flashes that fade naturally.

### Overlay Layers (additive, applied after the chain)

| Class | Trigger | Instance Data |
|---|---|---|
| `HitFlashLayer` | `Trigger(Color color, float duration)` | color + remaining time — up to 4 simultaneous |
| `AnimeSuperLayer` | `Trigger(Color color, float duration)` | color + remaining time — 1 at a time |

Overlays never read scene pixels. Their `Apply` draws a full-screen quad with `BlendState.Additive`.

## ScreenFXComponent Pipeline

Two ping-pong render targets are added alongside the existing `_sceneTarget`.

`EndCapture()` orchestration uses a `source` variable that starts pointing at `_sceneTarget`:

1. Iterate `DistortionLayers` in order. For each active layer: call `Apply(spriteBatch, source, destination)` then swap source/destination.
2. Set render target to null. Blit `source` to the back buffer with no effect. If no distortion layers were active, `source` is still `_sceneTarget`, so the unmodified scene is blitted — correct either way.
3. Iterate `OverlayLayers` in order. For each active layer: call `Apply(spriteBatch)` with additive blending.

Inactive layers (`IsActive == false`) are skipped entirely — no render target swap, no GPU pass.

## Public Surface on ScreenFXComponent

```csharp
// Ordered lists — user can insert custom layers or reorder
public List<IDistortionLayer> DistortionLayers { get; }
public List<IOverlayLayer> OverlayLayers { get; }

// Direct access to built-in layers
public ForceRippleLayer ForceRipple { get; }
public GravityWaveLayer GravityWave { get; }
public ScreenShakeLayer ScreenShake { get; }
public ChromaticAberrationLayer ChromaticAberration { get; }
public HeatHazeLayer HeatHaze { get; }
public HitFlashLayer HitFlash { get; }
public AnimeSuperLayer AnimeSuper { get; }

// Convenience pass-throughs
public void TriggerForceRipple(Vector2 position, float strength = 1f);
public void TriggerGravityWave(Vector2 position, float strength = 1f);
public void TriggerScreenShake(float trauma);
public void TriggerChromaticAberration(float intensity, float duration);
public void TriggerHeatHaze(float intensity, float duration);
public void TriggerHitFlash(Color color, float duration);
public void TriggerAnimeSuper(Color color, float duration);
```

## File Structure

```
ScreenFXBuddy/
├── Effects/
│   ├── IDistortionLayer.cs
│   ├── IOverlayLayer.cs
│   ├── ForceRippleLayer.cs
│   ├── GravityWaveLayer.cs
│   ├── ScreenShakeLayer.cs
│   ├── ChromaticAberrationLayer.cs
│   ├── HeatHazeLayer.cs
│   ├── HitFlashLayer.cs
│   └── AnimeSuperLayer.cs
└── ScreenFXComponent.cs   ← updated
```

Each layer class lives in its own file and is self-contained: it holds instance state, owns its `Effect` reference (loaded in `LoadContent`), and drives its own shader parameters.

## Debug / Testing Strategy

Each layer is initially implemented with `Debug_Red.fx` (or a tinted variant) so the pipeline can be validated before any real shader math is written. Once a solid color appears for each layer in isolation, real shaders are substituted one at a time.
