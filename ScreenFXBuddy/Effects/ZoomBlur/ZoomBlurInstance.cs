using Microsoft.Xna.Framework;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class ZoomBlurInstance
{
    public Vector2 Position { get; private set; }
    public float PeakStrength { get; private set; }
    public float Radius { get; private set; }

    public CountdownTimer Timer { get; private set; } = new CountdownTimer();

    public bool IsAlive => Timer.HasTimeRemaining;

    public ZoomBlurInstance(
        Vector2 position,
        float peakStrength = 0.05f,
        float radius = 1.0f,
        float duration = 0.4f)
    {
        Position = position;
        PeakStrength = peakStrength;
        Radius = radius;
        Timer.Start(duration);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }
}
