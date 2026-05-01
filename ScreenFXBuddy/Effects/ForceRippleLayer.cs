using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// This is a screen effect that adds circular ripples that shoot out from a point
/// </summary>
public class ForceRippleLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private readonly List<RippleInstance> _ripples = new();

    /// <summary>
    /// The max number of ripples that can be displayed by the shader.
    /// It will keep adding them, but the ripple shader only supports this many ripple effects.
    /// </summary>
    private const int MaxInstances = 16;

    public bool IsActive => _ripples.Count > 0;

    public ForceRippleLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Color");
    }

    public void Trigger(Vector2 position, float strength = 5f, float speed = 25f, float size = 10f, float time = 2f)
    {
        _ripples.Add(new RippleInstance(position, strength, speed, size, time));
    }

    public void Update(GameTime gameTime)
    {
        var i = 0;
        while (i < _ripples.Count)
        {
            _ripples[i].Update(gameTime);
            if (!_ripples[i].IsAlive)
            {
                _ripples.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        //TODO: go through the list of ripples and set the shader effect

        //TODO: make sure not to set more than MaxInstances ripples in the shader.

        _graphicsDevice.SetRenderTarget(destination);

        _effect.Parameters["DebugColor"].SetValue(Color.Yellow.ToVector4());
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
