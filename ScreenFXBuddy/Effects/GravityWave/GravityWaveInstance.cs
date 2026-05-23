using Microsoft.Xna.Framework;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class GravityWaveInstance
{
    public Vector2 Position { get; private set; }
    public float Strength { get; private set; }
    public float StartHeight { get; private set; }
    public float EndHeight { get; private set; }
    public float Speed { get; private set; }

    public CountdownTimer Timer { get; private set; } = new CountdownTimer();

    public bool IsAlive => Timer.HasTimeRemaining;

    public GravityWaveInstance(
        Vector2 position,
        float strength = 0.04f,
        float startHeight = 0.05f,
        float endHeight = 0.25f,
        float speed = 0.5f,
        float duration = 1.5f)
    {
        Position = position;
        Strength = strength;
        StartHeight = startHeight;
        EndHeight = endHeight;
        Speed = speed;
        Timer.Start(duration);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }
}
