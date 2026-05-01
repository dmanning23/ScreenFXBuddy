using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// This is a screen effect that adds circular ripples that shoot out from a point.
/// Each RippleInstance independently controls its own position, speed, size, and lifetime.
/// Ring geometry (outer/inner radius) is computed here each frame; the shader only draws rings.
/// </summary>
public class ForceRippleLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    // Cached parameter handles — set once in LoadContent
    private EffectParameter _pRippleCount;
    private EffectParameter _pRipplePositions;
    private EffectParameter _pRippleRings;
    private EffectParameter _pAspectRatio;

    // Reused each frame to avoid allocations.
    // float4 is used for both arrays to avoid float2/float3 GLSL packing quirks.
    private readonly Vector4[] _positionBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _ringBuffer      = new Vector4[MaxInstances];

    private readonly List<RippleInstance> _ripples = new();

    /// <summary>
    /// The max number of ripples the shader can render simultaneously.
    /// Additional ripples are accepted but only the first MaxInstances are rendered.
    /// </summary>
    private const int MaxInstances = 16;

    public bool IsActive => _ripples.Count > 0;

    public ForceRippleLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_Ripple");
        _pRippleCount     = _effect.Parameters["RippleCount"];
        _pRipplePositions = _effect.Parameters["RipplePositions"];
        _pRippleRings     = _effect.Parameters["RippleRings"];
        _pAspectRatio     = _effect.Parameters["AspectRatio"];
    }

    /// <summary>
    /// Spawn a new ripple.
    /// </summary>
    /// <param name="position">UV-space origin [0,1] of the ripple center.</param>
    /// <param name="strength">UV-space displacement at the ring crest (0.02 = subtle, 0.1 = strong).</param>
    /// <param name="speed">Outward expansion speed in UV units per second.</param>
    /// <param name="size">Ring thickness in UV units.</param>
    /// <param name="time">Lifetime in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.05f, float speed = 0.4f, float size = 0.08f, float time = 2f)
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
                _ripples.RemoveAt(i);
            else
                i++;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        float aspectRatio = (float)_graphicsDevice.Viewport.Width / _graphicsDevice.Viewport.Height;

        int count = Math.Min(_ripples.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var r = _ripples[i];

            // How far the ripple has travelled outward from its origin
            float elapsed     = r.TotalTime - r.Timer.RemainingTime;
            float outerRadius = elapsed * r.Speed;
            float innerRadius = Math.Max(0f, outerRadius - r.Size);

            // Calculate the position
            var x = r.Position.X / (float)_graphicsDevice.Viewport.Width;
            var y = r.Position.Y / (float)_graphicsDevice.Viewport.Height;

            //Calculat the remaining strength
            var remainingStrength = r.Strength * r.Timer.Lerp;

            _positionBuffer[i] = new Vector4(x, y, 0f, 0f);
            _ringBuffer[i]     = new Vector4(outerRadius, innerRadius, remainingStrength, 0f);
        }

        _pRippleCount.SetValue((float)count);
        _pRipplePositions.SetValue(_positionBuffer);
        _pRippleRings.SetValue(_ringBuffer);
        _pAspectRatio.SetValue(aspectRatio);

        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
