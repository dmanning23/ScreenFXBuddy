using GameTimer;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Effects;

public class ElectricInstance
{
    /// <summary>
    /// The screen position of the center of this ripple
    /// This is measured in the x/y pixel coordinates
    /// </summary>
    public Vector2 Position { get; set; }

    public Color Color { get; set; }

    public float Radius { get; set; }

    /// <summary>
    /// Total lifetime of this ripple in seconds, set at construction time.
    /// Used to compute normalized age for the shader.
    /// </summary>
    public float TotalTime { get; private set; }

    /// <summary>
    /// Used to time the ripple effect from start to finish
    /// </summary>
    public CountdownTimer Timer { get; protected set; } = new CountdownTimer();

    /// <summary>
    /// Whether or not this ripple is still alive or can be removed from the game
    /// </summary>
    public bool IsAlive => Timer.HasTimeRemaining;

    public ElectricInstance(Vector2 position, Color color, float radius = 0.25f, float time = 1.5f)
    {
        Position = position;
        Color = color;
        Radius = radius;
        TotalTime = time;
        Timer.Start(time);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }
}
