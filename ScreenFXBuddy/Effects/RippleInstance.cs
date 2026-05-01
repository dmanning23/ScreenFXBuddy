using GameTimer;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// A single screen ripple
/// </summary>
public class RippleInstance
{
    /// <summary>
    /// The screen position of the center of this ripple
    /// This is measured in the x/y pixel coordinates
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// How strong this ripple effect is
    /// This will determine the amonut of distortion to add to the shader effect
    /// </summary>
    public float Strength { get; set; }

    /// <summary>
    /// How fast this ripple is shooting out from the position
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// How big this ripple is from the inside to the outside radius
    /// Measured in pixels
    /// </summary>
    public float Size { get; set; }

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

    public RippleInstance(Vector2 position, float strength = 0.05f, float speed = 0.4f, float size = 0.08f, float time = 2f)
    {
        Position = position;
        Strength = strength;
        Speed = speed;
        Size = size;
        TotalTime = time;
        Timer.Start(time);
    }

    public void Update(GameTime gameTime)
    {
        Timer.Update(gameTime);
    }
}
