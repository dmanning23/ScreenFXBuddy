using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class GravityWaveLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private readonly List<WaveInstance> _instances = new();

    private const int MaxInstances = 8;

    public bool IsActive => _instances.Count > 0;

    public GravityWaveLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Red");
    }

    public void Trigger(Vector2 position, float strength = 1f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new WaveInstance(position, strength, 0f));
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Age = inst.Age + dt };
            if (inst.Age >= 1f)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }

    private record struct WaveInstance(Vector2 Position, float Strength, float Age);
}
