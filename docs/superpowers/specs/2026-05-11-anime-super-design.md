# AnimeSuperLayer — Super Flash Design Spec

Date: 2026-05-11

## Overview

Complete the existing `AnimeSuperLayer` stub as a full-screen color flash with a configurable flash-hold-fade envelope. When a super move activates, the screen instantly (or rapidly) cuts to a chosen color, holds at peak opacity, then fades back out. The classic Street Fighter super flash moment — or a red burst on a powerful hit, or a blue flash for an ice super.

The existing stub's `Trigger(Color color, float duration)` signature is replaced with a three-phase envelope. No shader is required — this is a pure SpriteBatch draw using a 1×1 white pixel tinted by `color` with additive blend.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `ScreenFXBuddy/Effects/AnimeSuperLayer.cs` | Replace stub with flash-hold-fade implementation |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Update `TriggerAnimeSuper` signature |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

No new shader or `.mgcb` entry needed — the existing `_whitePixel` + SpriteBatch is sufficient.

---

## AnimeSuperLayer

Replaces the stub. Implements `IOverlayLayer, IDisposable`.

**State fields:**
```csharp
private Color  _color;
private float  _flashIn;   // seconds to ramp from 0 → 1
private float  _hold;      // seconds at peak opacity
private float  _fadeOut;   // seconds to ramp from 1 → 0
private float  _age;       // elapsed time since trigger
private bool   _active;
```

`public bool IsActive => _active;`

**`LoadContent`:** creates `_whitePixel` (1×1 white `Texture2D`), loads no effect (removed the `Debug_Color` reference entirely — no `_effect` field needed).

**`Trigger`:**
```csharp
public void Trigger(
    Color color,
    float flashIn  = 0.05f,
    float hold     = 0.30f,
    float fadeOut  = 0.40f)
```
Sets all fields, sets `_age = 0`, `_active = true`. Always replaces any in-progress flash.

**`Update`:** increments `_age`. Sets `_active = false` when `_age >= flashIn + hold + fadeOut`.

**`Apply`:**

Compute `alpha` from the current phase:
```csharp
float alpha;
if (_age < _flashIn)
    // flash-in phase: 0 → 1
    alpha = _flashIn > 0 ? _age / _flashIn : 1f;
else if (_age < _flashIn + _hold)
    // hold phase: 1
    alpha = 1f;
else
    // fade-out phase: 1 → 0
    alpha = 1f - (_age - _flashIn - _hold) / _fadeOut;

alpha = MathHelper.Clamp(alpha, 0f, 1f);
```

Draw:
```csharp
spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, ...);
spriteBatch.Draw(_whitePixel, viewport.Bounds, _color * alpha);
spriteBatch.End();
```

**`Dispose`:** disposes `_whitePixel`.

---

## ScreenFXComponent Changes

Replace the existing `TriggerAnimeSuper` method:

```csharp
public void TriggerAnimeSuper(
    Color color,
    float flashIn  = 0.05f,
    float hold     = 0.30f,
    float fadeOut  = 0.40f)
    => AnimeSuper.Trigger(color, flashIn, hold, fadeOut);
```

---

## Example Project

```csharp
// F1: white super flash — classic SF super activation
if (keys.IsKeyDown(Keys.F1) && !_prevKeys.IsKeyDown(Keys.F1))
    _screenFX.TriggerAnimeSuper(Color.White);

// F2: red flash — heavy hit / critical moment
if (keys.IsKeyDown(Keys.F2) && !_prevKeys.IsKeyDown(Keys.F2))
    _screenFX.TriggerAnimeSuper(new Color(255, 30, 30),
        flashIn: 0f, hold: 0.1f, fadeOut: 0.5f);

// F3: blue flash — ice super / power-up
if (keys.IsKeyDown(Keys.F3) && !_prevKeys.IsKeyDown(Keys.F3))
    _screenFX.TriggerAnimeSuper(new Color(60, 120, 255),
        flashIn: 0.1f, hold: 0.4f, fadeOut: 0.6f);
```

---

## Visual Design Notes

- **Additive blend:** the flash brightens the scene rather than covering it. White at full opacity washes out to pure white; a saturated color tints the scene toward that hue. This matches the look of real fighting game super flashes.
- **`flashIn = 0`:** passing zero is valid — `alpha` jumps to 1 immediately on the first frame. Use for hard cuts.
- **Single instance:** always replaces. Two simultaneous super flashes is not a supported use case.
- **No shader:** the entire effect is a single `spriteBatch.Draw` call. Fast, zero GPU overhead.
