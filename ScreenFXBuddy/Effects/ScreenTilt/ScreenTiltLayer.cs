using System;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ScreenTiltLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pAngle;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private record struct TiltInstance(
        float MaxAngle,
        float Duration,
        float Age);

    private TiltInstance? _instance;

    private const float SnapFraction = 0.1f;

    public bool IsActive => _instance.HasValue;

    public ScreenTiltLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_ScreenTilt");
        _pAngle        = _effect.Parameters["Angle"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="angle">Peak rotation in degrees. Positive = clockwise. Try 2–5 for subtle, 5–8 for dramatic.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(float angle = 3.0f, float duration = 0.4f)
    {
        _instance = new TiltInstance(angle, duration, 0f);
    }

    public void Update(GameClock clock)
    {
        if (!_instance.HasValue) return;
        var inst = _instance.Value;
        inst = inst with { Age = inst.Age + clock.TimeDelta };
        _instance = inst.Age >= inst.Duration ? null : inst;
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!_instance.HasValue) return;
        var inst = _instance.Value;

        float t = inst.Age / inst.Duration;

        float currentAngleDeg;
        if (t < SnapFraction)
        {
            currentAngleDeg = inst.MaxAngle * (t / SnapFraction);
        }
        else
        {
            float easeT = (t - SnapFraction) / (1f - SnapFraction);
            currentAngleDeg = inst.MaxAngle * (1f - easeT * easeT);
        }

        var vp = _graphicsDevice.Viewport;
        _graphicsDevice.SetRenderTarget(destination);

        _pAngle.SetValue(MathHelper.ToRadians(currentAngleDeg));
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);
        _pSceneTexture.SetValue(source);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
