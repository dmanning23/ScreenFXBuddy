using System;
using GameTimer;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// The different ways that the hit flash is applied to the screen
/// </summary>
public enum FadeMode
{
    FadeIn,    // blend color starts at 0.0 and goes to 1.0
    FadeOut,   // blend color starts at 1.0 and goes to 0.0
    Constant   // blend color is always 1.0
}

/// <summary>
/// The curve that the hit flash will follow when fading in/out.
/// </summary>
public enum FadeCurve
{
    Linear,       // constant rate of change
    Logarithmic,  // fast initial change that decelerates (log curve)
    Exponential   // slow initial change that accelerates (quadratic ease-in)
}

/// <summary>
/// The way that the Hit Flash colors are applied to the screen.
/// </summary>
public enum FlashBlendMode
{
    Multiply,      // Multiplies the color values of the screen and blend color layers
    ColorBurn,     // Darkens the screen to reflect the blend color by increasing contrast
    LinearBurn,    // Darkens the screen by decreasing brightness
    Screen,        // Results in a lighter color similar to projecting multiple slides onto one screen
    ColorDodge,    // Lightens the screen to reflect the blend color by decreasing contrast
    LinearDodge,   // Simply adds the color values together
}

/// <summary>
/// A single screen HitFlash
/// </summary>
public class HitFlashInstance
{
    public float TotalTime { get; private set; }

    public CountdownTimer Timer { get; protected set; } = new CountdownTimer();

    public bool IsAlive => Timer.HasTimeRemaining;

    public Color BlendColor { get; set; }

    public FadeMode FadeMode { get; set; }

    public FadeCurve FadeCurve { get; set; }

    public FlashBlendMode FlashBlendMode { get; set; }

    public HitFlashInstance(Color blendColor,
        FadeMode mode = FadeMode.FadeOut,
        FadeCurve curve = FadeCurve.Linear,
        FlashBlendMode blendMode = FlashBlendMode.LinearDodge,
        float time = 1f)
    {
        BlendColor = blendColor;
        FadeMode = mode;
        FadeCurve = curve;
        FlashBlendMode = blendMode;
        TotalTime = time;
        Timer.Start(time);
    }

    public void Update(GameTime gameTime)
    {
        Timer.Update(gameTime);
    }

    public Color GetCurrentColor()
    {
        float amount = FadeMode switch
        {
            FadeMode.FadeIn  => ApplyCurve(1f - Timer.Lerp),  // 0 → 1
            FadeMode.FadeOut => ApplyCurve(Timer.Lerp),        // 1 → 0
            _                => 1f                             // Constant
        };

        return BlendColor * amount;
    }

    // t is expected in [0, 1]; output is in [0, 1].
    // Logarithmic: rises fast then levels off (log curve, concave down).
    // Exponential: rises slowly then accelerates (quadratic, concave up).
    private float ApplyCurve(float t) => FadeCurve switch
    {
        FadeCurve.Logarithmic => MathF.Log(1f + t * (MathF.E - 1f)),
        FadeCurve.Exponential => t * t,
        _                     => t
    };
}
