using System;
using GameTimer;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Effects;

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

    public EffectBlendMode FlashBlendMode { get; set; }

    public HitFlashInstance(Color blendColor,
        FadeMode mode = FadeMode.FadeOut,
        FadeCurve curve = FadeCurve.Linear,
        EffectBlendMode blendMode = EffectBlendMode.LinearDodge,
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
            FadeMode.FadeIn => ApplyCurve(1f - Timer.Lerp),  // 0 → 1
            FadeMode.FadeOut => ApplyCurve(Timer.Lerp),        // 1 → 0
            _ => 1f                             // Constant
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
        _ => t
    };
}
