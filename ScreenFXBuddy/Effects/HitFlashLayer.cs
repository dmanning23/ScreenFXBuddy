using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class HitFlashLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private readonly List<FlashInstance> _instances = new();

    private const int MaxInstances = 4;

    public bool IsActive => _instances.Count > 0;

    public HitFlashLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Color");
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Color color, float duration)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new FlashInstance(color, duration, duration));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Remaining = inst.Remaining - dt };
            if (inst.Remaining <= 0f)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        foreach (var flash in _instances)
        {
            float alpha = flash.Remaining / flash.Duration;

            _effect.Parameters["DebugColor"].SetValue(Color.Indigo.ToVector4());
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                _effect);
            spriteBatch.Draw(_whitePixel, _graphicsDevice.Viewport.Bounds,
                flash.Color * alpha);
            spriteBatch.End();
        }
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }

    private record struct FlashInstance(Color Color, float Duration, float Remaining);
}
