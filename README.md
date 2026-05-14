# ScreenFXBuddy

A MonoGame library of post-processing screen effects built for fighting games. Drop it in, trigger effects from game code, and let the component handle the render pipeline.

## Effects

### Distortion Effects
These warp scene pixels through a ping-pong render target chain. Multiple instances of multi-instance effects can run simultaneously.

| Effect | Description | Instances |
|---|---|---|
| **ForceRipple** | Shockwave distortion radiating from an impact point | 4 |
| **GravityWave** | Concentric expanding wave for heavy or super moves | 4 |
| **ScreenShake** | Trauma-based camera shake with configurable decay | 1 |
| **ChromaticAberration** | Sustained RGB channel split, intensity-driven | 1 |
| **ChromaticSplit** | Snap-and-return RGB split (sin curve) | 1 |
| **HeatHaze** | Layered-sine heat shimmer column rising from a point | 8 |
| **FreezeFrame** | Desaturate + tint + radial vignette flash | 1 |
| **ZoomBlur** | Radial zoom blur push-and-return | 1 |
| **ScreenTilt** | Screen rotation with snap-and-ease-back | 1 |
| **Vortex** | Inverse-distance UV rotation swirl | 4 |
| **GlassShatter** | Procedural Voronoi crack distortion with crack lines | 1 |

### Overlay Effects
These are drawn additively over the backbuffer without reading scene pixels.

| Effect | Description | Instances |
|---|---|---|
| **HitFlash** | Additive full-screen flash on hit confirmation | 1 |
| **AnimeSuper** | Stylized full-screen flash for super/ultimate moves | 1 |
| **Letterbox** | Cinematic black bars sliding in and out | 1 |
| **SpeedLines** | Radial speed lines expanding from a point | 1 |
| **Electric** | FBM polar-noise arcing tendrils | 4 |
| **Frost** | FBM crystal overlay expanding radially from origin | 1 |
| **Smoke** | FBM procedural smoke cloud drifting upward | 4 |

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
// Distortion
_screenFX.TriggerForceRipple(hitPosition);
_screenFX.TriggerScreenShake();
_screenFX.TriggerGlassShatter(hitPosition);
_screenFX.TriggerFreezeFrame(new Color(100, 160, 255));   // icy blue freeze
_screenFX.TriggerVortex(hitPosition, speed: -2.0f);       // counter-clockwise

// Overlay
_screenFX.TriggerHitFlash(Color.White);
_screenFX.TriggerAnimeSuper(new Color(255, 220, 50));     // gold ultra flash
_screenFX.TriggerElectric(hitPosition, new Color(100, 200, 255));
_screenFX.TriggerFrost(hitPosition, new Color(180, 220, 255));
_screenFX.TriggerSmoke(hitPosition, Color.Gray);
```

## How It Works

Each frame, the scene is drawn to an offscreen render target. Active distortion effects are applied in sequence using a ping-pong render target chain — one GPU pass per effect type. Overlay effects (HitFlash, Electric, Smoke, etc.) are then composited additively on top without reading scene pixels.

The `IScreenFXService` interface is registered in `Game.Services` so any game system can trigger effects without a direct reference to the component.

## License

MIT
