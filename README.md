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
_screenFX = new ScreenFXComponent(this);
Components.Add(_screenFX);
```

Wrap your `Draw` method with `BeginCapture` / `EndCapture` so the component can intercept the scene:

```csharp
protected override void Draw(GameTime gameTime)
{
    _screenFX.BeginCapture();

    GraphicsDevice.Clear(Color.Black);
    // draw your scene here

    _screenFX.EndCapture();
    base.Draw(gameTime);
}
```

Trigger effects from anywhere in game code:

```csharp
_screenFX.TriggerForceRipple(hitPosition);
_screenFX.TriggerScreenShake(trauma: 0.8f);
_screenFX.TriggerHitFlash(Color.White);
```

## How It Works

Each frame, the scene is drawn to an offscreen render target. Active distortion effects are then applied in sequence using a ping-pong render target chain (one GPU pass per effect type). Additive overlay effects (HitFlash, AnimeSuper) are composited on top without reading scene pixels.

## License

MIT
