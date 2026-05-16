using GameTimer;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// A single screen ripple
/// </summary>
public class SmokeInstance
{
    //Vector2 Position, Vector4 Color, float Radius, float Duration
    /// <summary>
    /// The screen position of the center of this ripple
    /// This is measured in the x/y pixel coordinates
    /// </summary>
    public Vector2 Position { get; set; }

    public Color Color { get; set; }

    public float Radius { get; set; }

    /// <summary>
    /// Used to time the ripple effect from start to finish
    /// </summary>
    public CountdownTimer Timer { get; protected set; } = new CountdownTimer();

    /// <summary>
    /// Whether or not this ripple is still alive or can be removed from the game
    /// </summary>
    public bool IsAlive => Timer.HasTimeRemaining;

    public SmokeInstance(Vector2 position, Color color, float radius = 0.15f, float duration = 2.0f)
    {
        Position = position;
        Color = color;
        Radius = radius;
        Timer.Start(duration);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }
}
