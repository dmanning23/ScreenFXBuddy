using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class AnimeSuperLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _whitePixel;

    private Color _color;
    private float _flashIn;
    private float _hold;
    private float _fadeOut;
    private float _age;
    private bool _active;

    public bool IsActive => _active;

    public AnimeSuperLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="color">Flash color. White = pure blinding flash; use tints for themed supers.</param>
    /// <param name="flashIn">Seconds to ramp from 0 → full alpha.</param>
    /// <param name="hold">Seconds at full alpha before fading.</param>
    /// <param name="fadeOut">Seconds to fade from full alpha → 0.</param>
    public void Trigger(Color color, float flashIn = 0.05f, float hold = 0.30f, float fadeOut = 0.40f)
    {
        _color   = color;
        _flashIn = flashIn;
        _hold    = hold;
        _fadeOut = fadeOut;
        _age     = 0f;
        _active  = true;
    }

    public void Update(GameClock clock)
    {
        if (!_active) return;
        _age += clock.TimeDelta;
        if (_age >= _flashIn + _hold + _fadeOut)
            _active = false;
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        float alpha;
        if (_age < _flashIn)
            alpha = _flashIn > 0f ? _age / _flashIn : 1f;
        else if (_age < _flashIn + _hold)
            alpha = 1f;
        else
        {
            float fadeProgress = _age - _flashIn - _hold;
            alpha = _fadeOut > 0f ? 1f - fadeProgress / _fadeOut : 0f;
        }

        var vp = _graphicsDevice.Viewport;
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        spriteBatch.Draw(_whitePixel, vp.Bounds, _color * alpha);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
