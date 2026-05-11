# LetterboxLayer — Design Spec

Date: 2026-05-11

## Overview

Implement `LetterboxLayer` — black bars that slide in from the top and bottom edges of the screen, hold, then slide back out. Used to create a cinematic widescreen feel during super move freeze frames, dramatic moments, or cutscene transitions.

This is a new `IOverlayLayer`. No shader required — two `SpriteBatch.Draw` calls with a 1×1 black pixel rect. A state machine drives the bar animation through four phases: `SlidingIn → Holding → SlidingOut → Idle`.

---

## Modified / Created Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ScreenFXBuddy/Effects/LetterboxLayer.cs` | State machine + animated bar drawing |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `Letterbox` property and `TriggerLetterbox` method |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

No shader or `.mgcb` entry needed.

---

## LetterboxLayer

New class. Implements `IOverlayLayer, IDisposable`.

**State enum:**
```csharp
private enum LetterboxState { Idle, SlidingIn, Holding, SlidingOut }
```

**State fields:**
```csharp
private LetterboxState _state = LetterboxState.Idle;
private float _barHeight;    // target bar height as fraction of viewport height (0–0.5)
private float _slideIn;      // duration of slide-in phase
private float _hold;         // duration of hold phase
private float _slideOut;     // duration of slide-out phase
private float _phaseAge;     // time elapsed in the current phase
private float _currentHeight; // current animated bar height (fraction of viewport height)
```

`public bool IsActive => _state != LetterboxState.Idle;`

**`LoadContent`:** creates `_blackPixel` (1×1 black `Texture2D`).

**`Trigger`:**
```csharp
public void Trigger(
    float barHeight = 0.10f,
    float slideIn   = 0.15f,
    float hold      = 1.00f,
    float slideOut  = 0.15f)
```
Sets all parameters, resets `_phaseAge = 0`, `_currentHeight = 0`, transitions to `SlidingIn`. Always replaces any in-progress letterbox.

**`Update`:**

```csharp
float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
_phaseAge += dt;

switch (_state)
{
    case LetterboxState.SlidingIn:
        _currentHeight = _slideIn > 0
            ? MathHelper.Clamp(_phaseAge / _slideIn, 0f, 1f) * _barHeight
            : _barHeight;
        if (_phaseAge >= _slideIn) { _phaseAge = 0; _state = LetterboxState.Holding; }
        break;

    case LetterboxState.Holding:
        _currentHeight = _barHeight;
        if (_phaseAge >= _hold) { _phaseAge = 0; _state = LetterboxState.SlidingOut; }
        break;

    case LetterboxState.SlidingOut:
        _currentHeight = MathHelper.Clamp(1f - _phaseAge / _slideOut, 0f, 1f) * _barHeight;
        if (_phaseAge >= _slideOut) { _currentHeight = 0; _state = LetterboxState.Idle; }
        break;
}
```

**`Apply`:**

```csharp
int barPixels = (int)(_currentHeight * viewport.Height);
if (barPixels <= 0) return;

spriteBatch.Begin();

// Top bar
spriteBatch.Draw(_blackPixel,
    new Rectangle(0, 0, viewport.Width, barPixels), Color.Black);

// Bottom bar
spriteBatch.Draw(_blackPixel,
    new Rectangle(0, viewport.Height - barPixels, viewport.Width, barPixels), Color.Black);

spriteBatch.End();
```

**`Dispose`:** disposes `_blackPixel`.

---

## ScreenFXComponent Changes

```csharp
public LetterboxLayer Letterbox { get; private set; }

public void TriggerLetterbox(
    float barHeight = 0.10f,
    float slideIn   = 0.15f,
    float hold      = 1.00f,
    float slideOut  = 0.15f)
    => Letterbox.Trigger(barHeight, slideIn, hold, slideOut);
```

Register `Letterbox` in `OverlayLayers`.

---

## Example Project

```csharp
// F4: default letterbox — super moment timing
if (keys.IsKeyDown(Keys.F4) && !_prevKeys.IsKeyDown(Keys.F4))
    _screenFX.TriggerLetterbox();

// F5: thick bars — dramatic cutscene feel
if (keys.IsKeyDown(Keys.F5) && !_prevKeys.IsKeyDown(Keys.F5))
    _screenFX.TriggerLetterbox(barHeight: 0.18f, slideIn: 0.2f, hold: 1.5f, slideOut: 0.3f);

// F6: quick snap — fast cinematic cut
if (keys.IsKeyDown(Keys.F6) && !_prevKeys.IsKeyDown(Keys.F6))
    _screenFX.TriggerLetterbox(barHeight: 0.10f, slideIn: 0f, hold: 0.5f, slideOut: 0.1f);
```

---

## Visual Design Notes

- **`barHeight` range:** 0.08–0.12 gives a standard cinematic widescreen feel. 0.18+ creates a more severe "extreme close-up" crop. Values above 0.4 would cover most of the screen and are not recommended.
- **`slideIn = 0`:** valid — bars snap to full height instantly on the first frame, like a hard cut.
- **Easing:** the current implementation uses linear interpolation. If a more polished slide-in curve is desired in the future, replace the linear `_phaseAge / _slideIn` with a smoothstep: `t * t * (3 - 2 * t)`. Linear is specified here for simplicity.
- **No alpha:** bars are always opaque black. A semi-transparent letterbox is unusual in fighting games and not supported.
- **Pairing with other effects:** `LetterboxLayer` and `AnimeSuperLayer` are commonly triggered together (`TriggerLetterbox()` + `TriggerAnimeSuper(Color.White)`) but remain separate calls — the caller composes them.
