using GameTimer;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ChromaticAberrationLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    /// <summary>
    /// screen-pixel coords, converted to UV in Apply
    /// </summary>
    private Vector2 _startPosition;

    /// <summary>
    /// max UV spread Distance at end of effect
    /// </summary>
    private float _distance;

    private enum AberrationMode { Sustained, Split }
    private AberrationMode _mode = AberrationMode.Sustained;

    private float _splitMaxDistance;
    private float _splitDuration;
    private float _splitAge;

    public FadeCurve FadeCurve { get; set; }

    protected CountdownTimer Timer { get; set; } = new CountdownTimer();

    public bool IsActive => _mode == AberrationMode.Sustained
        ? !Timer.Paused && Timer.HasTimeRemaining
        : _splitAge < _splitDuration;

    public ChromaticAberrationLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_ChromaticAberration");
    }

    /// <param name="startPosition">Screen-pixel position the aberration radiates from.</param>
    /// <param name="distance">Max UV-space channel spread at end of effect. Try 0.05–0.2.</param>
    /// <param name="time">Duration in seconds.</param>4
    public void Trigger(Vector2 startPosition, float distance = 1f, float time = 2f, FadeCurve fadeCurve = FadeCurve.Linear)
    {
        _startPosition = startPosition;
        _distance = distance;
        FadeCurve = fadeCurve;
        Timer.Start(time);
        _mode = AberrationMode.Sustained;
    }

    /// <param name="position">Screen-pixel position the split radiates from.</param>
    /// <param name="maxDistance">Peak UV-space channel separation. Try 0.03–0.08.</param>
    /// <param name="duration">Total lifetime in seconds. Try 0.2–0.4 for a snappy hit.</param>
    public void TriggerSplit(Vector2 position, float maxDistance = 0.05f, float duration = 0.3f)
    {
        _startPosition    = position;
        _splitMaxDistance = maxDistance;
        _splitDuration    = duration;
        _splitAge         = 0f;
        _mode             = AberrationMode.Split;
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
        if (_mode == AberrationMode.Split)
            _splitAge = Math.Min(_splitAge + clock.TimeDelta, _splitDuration);
    }

    private float ApplyCurve(float t) => FadeCurve switch
    {
        FadeCurve.Logarithmic => MathF.Log(1f + t * (MathF.E - 1f)),
        FadeCurve.Exponential => t * t,
        _ => t
    };

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        var viewport = _graphicsDevice.Viewport;
        var originUV = new Vector2(
            _startPosition.X / viewport.Width,
            _startPosition.Y / viewport.Height);

        float currentDistance, currentStrength;

        if (_mode == AberrationMode.Sustained)
        {
            // Distance grows from 0 → _distance as the timer counts down.
            currentDistance = _distance * ApplyCurve(1f - Timer.Lerp);

            // Strength fades from 1 → 0 as the timer counts down.
            currentStrength = ApplyCurve(Timer.Lerp);
        }
        else
        {
            float t = _splitDuration > 0f ? _splitAge / _splitDuration : 1f;
            float curve = MathF.Sin(t * MathF.PI);
            currentDistance = _splitMaxDistance * curve;
            currentStrength = curve;
        }

        _graphicsDevice.SetRenderTarget(destination);
        _effect.Parameters["Origin"].SetValue(originUV);
        _effect.Parameters["Distance"].SetValue(currentDistance);
        _effect.Parameters["Strength"].SetValue(currentStrength);
        _effect.Parameters["SceneTexture"].SetValue(source);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
