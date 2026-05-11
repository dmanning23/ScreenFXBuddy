# ChromaticAberrationLayer — Chromatic Split Extension Design Spec

Date: 2026-05-11

## Overview

Extend the existing `ChromaticAberrationLayer` with a second trigger mode: `TriggerSplit`. Where the existing `Trigger` grows the channel separation outward and fades over a longer duration (a sustained ring), `TriggerSplit` is a punchy transient — channels fly apart to a peak and snap back in under a second.

No shader changes. The existing `Distorter_ChromaticAberration.fx` already accepts `Distance` and `Strength` as independent per-frame floats. `TriggerSplit` drives them via a `sin(t * π)` curve rather than the current countdown-timer approach.

---

## Modified Files

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `ScreenFXBuddy/Effects/ChromaticAberrationLayer.cs` | Add `_mode` flag and `TriggerSplit` method |
| Modify | `ScreenFXBuddy/ScreenFXComponent.cs` | Add `TriggerChromaticSplit` method |
| Modify | `ScreenFXBuddy.Example/Game1.cs` | Add test keybindings |

No shader or `.mgcb` changes required.

---

## ChromaticAberrationLayer Changes

### New fields

```csharp
private enum AberrationMode { Sustained, Split }
private AberrationMode _mode = AberrationMode.Sustained;

// Used only in Split mode
private float _splitMaxDistance;
private float _splitDuration;
private float _splitAge;
```

### New method: `TriggerSplit`

```csharp
/// <summary>
/// Transient chromatic split — channels fly apart and snap back.
/// </summary>
/// <param name="position">Screen-pixel position the split radiates from.</param>
/// <param name="maxDistance">Peak UV-space channel separation. Try 0.03–0.08.</param>
/// <param name="duration">Total lifetime in seconds. Try 0.2–0.4 for a snappy hit.</param>
public void TriggerSplit(Vector2 position, float maxDistance = 0.05f, float duration = 0.3f)
{
    _startPosition     = position;
    _splitMaxDistance  = maxDistance;
    _splitDuration     = duration;
    _splitAge          = 0f;
    _mode              = AberrationMode.Split;
}
```

The existing `Trigger` method remains unchanged and implicitly sets `_mode = AberrationMode.Sustained` at the end of its body:

```csharp
public void Trigger(Vector2 startPosition, float distance = 1f, float time = 2f,
    FadeCurve fadeCurve = FadeCurve.Linear)
{
    _startPosition = startPosition;
    _distance      = distance;
    FadeCurve      = fadeCurve;
    Timer.Start(time);
    _mode          = AberrationMode.Sustained;   // ← add this line
}
```

### Updated `IsActive`

```csharp
public bool IsActive => _mode == AberrationMode.Sustained
    ? (!Timer.Paused && Timer.HasTimeRemaining)
    : _splitAge < _splitDuration;
```

### Updated `Update`

Add to `Update`, after the existing `Timer.Update(gameTime)` call:

```csharp
if (_mode == AberrationMode.Split)
    _splitAge = Math.Min(_splitAge + (float)gameTime.ElapsedGameTime.TotalSeconds, _splitDuration);
```

### Updated `Apply`

Replace the existing distance/strength computation with a mode branch:

```csharp
float currentDistance, currentStrength;

if (_mode == AberrationMode.Sustained)
{
    // Existing behaviour — distance grows, strength fades
    currentDistance = _distance * ApplyCurve(1f - Timer.Lerp);
    currentStrength = ApplyCurve(Timer.Lerp);
}
else
{
    // Split mode — sin(t*π) curve: 0 → peak → 0
    float t = _splitDuration > 0f ? _splitAge / _splitDuration : 1f;
    float curve = MathF.Sin(t * MathF.PI);
    currentDistance = _splitMaxDistance * curve;
    currentStrength = curve;
}
```

The rest of `Apply` (parameter uploads and SpriteBatch draw) is unchanged.

---

## ScreenFXComponent Changes

Add alongside the existing `TriggerChromaticAberration`:

```csharp
public void TriggerChromaticSplit(
    Vector2 position,
    float maxDistance = 0.05f,
    float duration    = 0.3f)
    => ChromaticAberration.TriggerSplit(position, maxDistance, duration);
```

---

## Example Project

```csharp
// RightShift+Q: chromatic split — snappy hit flash
// (note: RightShift is already bound — use a free key instead)
// PageUp: default chromatic split
if (keys.IsKeyDown(Keys.PageUp) && !_prevKeys.IsKeyDown(Keys.PageUp))
    _screenFX.TriggerChromaticSplit(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f));

// PageDown: wide slow split — super impact
if (keys.IsKeyDown(Keys.PageDown) && !_prevKeys.IsKeyDown(Keys.PageDown))
    _screenFX.TriggerChromaticSplit(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        maxDistance: 0.09f, duration: 0.5f);

// Home: tight fast split — light hit
if (keys.IsKeyDown(Keys.Home) && !_prevKeys.IsKeyDown(Keys.Home))
    _screenFX.TriggerChromaticSplit(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f),
        maxDistance: 0.025f, duration: 0.2f);
```

---

## Visual Design Notes

- **No shader changes:** the split drives `Distance` and `Strength` to zero at both endpoints, so the effect is invisible at start and end with no pop or residual colour shift. The shader's existing `lerp(original, aberrated, Strength)` handles the clean fade.
- **Simultaneous with sustained:** calling `TriggerSplit` while a sustained aberration is active replaces it — both modes share the same layer. This is intentional; two simultaneous channel-split passes on the same layer would be confusing to reason about.
- **Direction of split:** identical to the sustained mode — R pushed outward, B pushed inward, G unchanged. The split radiates from the provided position.
- **Composing with other hit effects:** `TriggerChromaticSplit` + `TriggerZoomBlur` at the same impact point is a natural combination for a heavy hit. Both are independent layers and fire simultaneously without conflict.
