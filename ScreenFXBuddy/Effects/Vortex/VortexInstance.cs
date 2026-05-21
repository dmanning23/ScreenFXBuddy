using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class VortexInstance
{
    public Vector2 Position { get; private set; }
    public float Radius { get; private set; }
    private float Speed { get; set; }

    private FadeCurve FadeCurve { get; set; }

    private float SpinInTime { get; set; }
    private float SpinOutTime { get; set; }
    private float TotalTime => SpinInTime + SpinOutTime;
    private bool Clockwise { get; set; }

    private CountdownTimer Timer { get; set; } = new CountdownTimer();

    public bool IsAlive => Timer.HasTimeRemaining;

    public VortexInstance(
        Vector2 position,
        float radius = 0.25f,
        float speed = 1f,
        float spinInTime = 0.3f,
        float spinOutTime = 0.3f,
        FadeCurve fadeCurve = FadeCurve.Linear,
        bool clockwise = true)
    {
        Position = position;
        Radius = radius;
        Speed = speed;
        SpinInTime = spinInTime;
        SpinOutTime = spinOutTime;
        FadeCurve = fadeCurve;
        Timer.Start(TotalTime);
        Clockwise = clockwise;
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }

    public float SwirlAmount()
    {
        var swirlAmount = Speed * (Clockwise ? -1f : 1f);
        //is it fading in or out
        if (Timer.GetCurrentTime() >= SpinInTime)
        {
            //we are spinning out

            //how long have we been spinning out?
            var timeLength = Timer.GetCurrentTime() - SpinInTime;
            float timeDelta = ApplyCurve(1f - (timeLength / SpinOutTime));
            float swirl = swirlAmount * timeDelta;
            return swirl;
        }
        else
        {
            //we are spinning in

            //how long have we been spinning out?
            var timeLength = Timer.GetCurrentTime();
            float timeDelta = ApplyCurve(timeLength / SpinInTime);
            float swirl = swirlAmount * timeDelta;
            return swirl;
        }
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
