using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class LetterboxLayer : IOverlayLayer, IDisposable
{
    private enum State { Idle, SlidingIn, Holding, SlidingOut }

    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _blackPixel;

    private State _state = State.Idle;
    private float _barHeight;
    private float _slideIn;
    private float _hold;
    private float _slideOut;
    private float _stateAge;

    public bool IsActive => _state != State.Idle;

    public LetterboxLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _blackPixel = new Texture2D(_graphicsDevice, 1, 1);
        _blackPixel.SetData(new[] { Color.Black });
    }

    /// <param name="barHeight">Bar height as fraction of screen height. 0.10 = 10% each bar.</param>
    /// <param name="slideIn">Seconds for bars to slide in to full height.</param>
    /// <param name="hold">Seconds bars stay at full height.</param>
    /// <param name="slideOut">Seconds for bars to slide back out.</param>
    public void Trigger(float barHeight = 0.10f, float slideIn = 0.15f, float hold = 1.00f, float slideOut = 0.15f)
    {
        _barHeight = barHeight;
        _slideIn   = slideIn;
        _hold      = hold;
        _slideOut  = slideOut;
        _stateAge  = 0f;
        _state     = State.SlidingIn;
    }

    public void Update(GameClock clock)
    {
        if (_state == State.Idle) return;
        _stateAge += clock.TimeDelta;

        switch (_state)
        {
            case State.SlidingIn when _stateAge >= _slideIn:
                _state    = State.Holding;
                _stateAge -= _slideIn;
                break;
            case State.Holding when _stateAge >= _hold:
                _state    = State.SlidingOut;
                _stateAge -= _hold;
                break;
            case State.SlidingOut when _stateAge >= _slideOut:
                _state = State.Idle;
                break;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        if (_state == State.Idle) return;

        float currentFraction = _state switch
        {
            State.SlidingIn  => _slideIn  > 0f ? _barHeight * (_stateAge / _slideIn)        : _barHeight,
            State.Holding    => _barHeight,
            State.SlidingOut => _slideOut > 0f ? Math.Max(0f, _barHeight * (1f - _stateAge / _slideOut)) : 0f,
            _                => 0f
        };

        if (currentFraction <= 0f) return;

        var vp = _graphicsDevice.Viewport;
        int barPixels = (int)(currentFraction * vp.Height);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        spriteBatch.Draw(_blackPixel, new Rectangle(0, 0, vp.Width, barPixels), Color.White);
        spriteBatch.Draw(_blackPixel, new Rectangle(0, vp.Height - barPixels, vp.Width, barPixels), Color.White);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _blackPixel?.Dispose();
    }
}
