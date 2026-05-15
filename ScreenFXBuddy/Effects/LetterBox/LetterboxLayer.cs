using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class LetterboxLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _blackPixel;

    private float _barHeight;
    private float _slideIn;
    private float _hold;
    private float _slideOut;

    private CountdownTimer Timer { get; set; } = new CountdownTimer();

    public bool IsActive => !Timer.Paused && Timer.HasTimeRemaining;


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
        _slideIn = slideIn;
        _hold = hold;
        _slideOut = slideOut;

        Timer.Start(_slideIn + _hold + _slideOut);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        if (!IsActive)
        {
            return;
        }

        float currentFraction;
        if (Timer.CurrentTime < _slideIn)
        {
            currentFraction = _slideIn > 0f ? _barHeight * (Timer.CurrentTime / _slideIn) : _barHeight;
        }
        else if (Timer.CurrentTime < _slideIn + _hold)
        {
            currentFraction = _barHeight;
        }
        else
        {
            float fadeProgress = Timer.CurrentTime - _slideIn - _hold;
            currentFraction = _slideOut > 0f ? Math.Max(0f, _barHeight * (1f - fadeProgress / _slideOut)) : 0f;
        }

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
