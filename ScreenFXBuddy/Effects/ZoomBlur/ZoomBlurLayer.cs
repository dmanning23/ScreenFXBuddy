using System;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ZoomBlurLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pOrigin;
    private EffectParameter _pStrength;
    private EffectParameter _pRadius;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private record struct BlurInstance(
        Vector2 Origin,
        float PeakStrength,
        float Radius,
        float Duration,
        float Age);

    private BlurInstance? _instance;

    public bool IsActive => _instance.HasValue;

    public ZoomBlurLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_ZoomBlur");
        _pOrigin = _effect.Parameters["Origin"];
        _pStrength = _effect.Parameters["Strength"];
        _pRadius = _effect.Parameters["Radius"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="position">Pixel-space position the blur radiates from.</param>
    /// <param name="strength">Peak UV displacement. 0.05 is subtle; 0.15 is dramatic.</param>
    /// <param name="radius">UV-space radius of affected area. 1.0 = full screen.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.05f, float radius = 1.0f, float duration = 0.4f)
    {
        _instance = new BlurInstance(position, strength, radius, duration, 0f);
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
        if (!IsActive)
        {
            return;
        }

        var inst = _instance.Value;

        float t = inst.Age / inst.Duration;
        float currentStrength = inst.PeakStrength * MathF.Sin(t * MathF.PI);

        var vp = _graphicsDevice.Viewport;
        var originUV = new Vector2(inst.Origin.X / vp.Width, inst.Origin.Y / vp.Height);

        _graphicsDevice.SetRenderTarget(destination);

        _pOrigin.SetValue(originUV);
        _pStrength.SetValue(currentStrength);
        _pRadius.SetValue(inst.Radius);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);
        _pSceneTexture.SetValue(source);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
