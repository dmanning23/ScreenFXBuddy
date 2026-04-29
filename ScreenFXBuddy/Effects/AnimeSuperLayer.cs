using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class AnimeSuperLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private Color _color;
    private float _remaining;
    private float _duration;

    public bool IsActive => _remaining > 0f;

    public AnimeSuperLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Color color, float duration)
    {
        _color     = color;
        _duration  = duration;
        _remaining = duration;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _remaining = Math.Max(0f, _remaining - dt);
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        float alpha = _remaining / _duration;
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(_whitePixel, _graphicsDevice.Viewport.Bounds, _color * alpha);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
