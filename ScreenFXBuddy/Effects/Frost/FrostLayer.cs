using System;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class FrostLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;

    private EffectParameter _pOrigin;
    private EffectParameter _pTintColor;
    private EffectParameter _pRadius;
    private EffectParameter _pProgress;
    private EffectParameter _pAspectRatio;

    private Vector2 _origin;
    private Vector4 _tintColor;
    private float _radius;
    private float _duration;
    private float _age;
    private bool _active;
    private float _aspectRatio;

    public bool IsActive => _active;

    public FrostLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Overlay_Frost");
        _pOrigin = _effect.Parameters["Origin"];
        _pTintColor = _effect.Parameters["TintColor"];
        _pRadius = _effect.Parameters["Radius"];
        _pProgress = _effect.Parameters["Progress"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="position">Pixel-space origin of the frost spread.</param>
    /// <param name="tintColor">Frost color. (180,220,255) = icy blue; Color.White = pure white frost.</param>
    /// <param name="radius">Max spread radius in UV units. 0.25 = quarter-screen radius.</param>
    /// <param name="duration">Total effect duration in seconds. Frost is fully expanded at duration/2.</param>
    public void Trigger(Vector2 position, Color tintColor, float radius = 0.25f, float duration = 1.50f)
    {
        var vp = _graphicsDevice.Viewport;
        _origin = new Vector2(position.X / vp.Width, position.Y / vp.Height);
        _tintColor = tintColor.ToVector4();
        _radius = radius;
        _duration = duration;
        _age = 0f;
        _active = true;
        _aspectRatio = (float)vp.Width / vp.Height;
    }

    public void Update(GameClock clock)
    {
        if (!_active) return;
        _age += clock.TimeDelta;
        if (_age >= _duration)
            _active = false;
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        if (!IsActive)
        {
            return;
        }

        float progress = MathHelper.Clamp(_age / _duration, 0f, 1f);
        var vp = _graphicsDevice.Viewport;

        _pOrigin.SetValue(_origin);
        _pTintColor.SetValue(_tintColor);
        _pRadius.SetValue(_radius);
        _pProgress.SetValue(progress);
        _pAspectRatio.SetValue(_aspectRatio);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(_whitePixel, vp.Bounds, Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _effect = null;
        _whitePixel?.Dispose();
        _whitePixel = null;
    }
}
