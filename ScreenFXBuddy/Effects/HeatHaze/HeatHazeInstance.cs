using GameTimer;
using Microsoft.Xna.Framework;
using System;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// A single screen ripple
/// </summary>
public class HeatHazeInstance
{
    /// <summary>
    /// The screen position of the center of this ripple
    /// This is measured in the x/y pixel coordinates
    /// </summary>
    public Vector2 Position { get; set; }

    public float Strength { get; set; }

    private float _radius;
    public float Radius
    {
        get
        {
            return _radius;
        }
        set
        {
            _radius = Math.Max(value, 0.001f);
        }
    }
    
    //TODO: heat haze needs fadeIn, hold, fadeOut

    public float Height { get; set; }

    /// <summary>
    /// Total lifetime of this ripple in seconds, set at construction time.
    /// Used to compute normalized age for the shader.
    /// </summary>
    public float Duration { get; private set; }

    /// <summary>
    /// Used to time the ripple effect from start to finish
    /// </summary>
    public CountdownTimer Timer { get; protected set; } = new CountdownTimer();

    /// <summary>
    /// Whether or not this ripple is still alive or can be removed from the game
    /// </summary>
    public bool IsAlive => Timer.HasTimeRemaining;

    public HeatHazeInstance(
        Vector2 position,
        float strength = 0.02f,
        float radius = 0.15f,
        float height = 0.40f,
        float duration = 3.0f)
    {
        Position = position;
        Strength = strength;
        Radius = radius;
        Height = height;
        Duration = duration;
        Timer.Start(duration);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }
}
