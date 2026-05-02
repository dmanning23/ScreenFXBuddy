using System;
using GameTimer;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// Controls whether speed lines expand outward from center or appear at full extent immediately.
/// </summary>
public enum SpeedLinesMode
{
    Static,  // lines appear at full intensity immediately
    Expand   // lines expand outward from center over the lifetime
}

/// <summary>
/// Tracks per-burst state for a speed lines overlay, computing current alpha and inner radius each frame.
/// </summary>
public class SpeedLinesInstance
{
    public Vector2 PixelPosition { get; }
    public Color Color { get; }
    public SpeedLinesMode LinesMode { get; }
    public FadeMode FadeMode { get; }
    public FadeCurve FadeCurve { get; }
    public int LineCount { get; }
    /// <summary>
    /// Outer radius of the burst in corrected UV space (Y axis is the reference).
    /// 0.5 reaches the top/bottom screen edges. On a 16:9 viewport, use ~0.9 to reach
    /// left/right edges, or 1.0 for full-screen coverage.
    /// </summary>
    public float MaxRadius { get; }

    public CountdownTimer Timer { get; } = new CountdownTimer();
    public bool IsAlive => Timer.HasTimeRemaining;

    public SpeedLinesInstance(Vector2 pixelPosition, Color color,
        SpeedLinesMode linesMode, FadeMode fadeMode, FadeCurve fadeCurve,
        int lineCount, float maxRadius, float duration)
    {
        PixelPosition = pixelPosition;
        Color         = color;
        LinesMode     = linesMode;
        FadeMode      = fadeMode;
        FadeCurve     = fadeCurve;
        LineCount     = lineCount;
        MaxRadius     = maxRadius;
        Timer.Start(duration);
    }

    public void Update(GameTime gameTime) => Timer.Update(gameTime);

    // 0–1 intensity for the current frame, derived from FadeMode + FadeCurve + timer.
    public float CurrentAlpha => FadeMode switch
    {
        FadeMode.FadeIn  => ApplyCurve(1f - Timer.Lerp),
        FadeMode.FadeOut => ApplyCurve(Timer.Lerp),
        _                => 1f
    };

    // UV-space inner radius. Static = 0 always. Expand = grows from 0 → MaxRadius.
    // Timer.Lerp is 1.0 at start and 0.0 at expiry.
    public float CurrentInnerRadius => LinesMode == SpeedLinesMode.Expand
        ? MaxRadius * (1f - Timer.Lerp)
        : 0f;

    private float ApplyCurve(float t) => FadeCurve switch
    {
        FadeCurve.Logarithmic => MathF.Log(1f + t * (MathF.E - 1f)),
        FadeCurve.Exponential => t * t,
        _                     => t
    };
}
