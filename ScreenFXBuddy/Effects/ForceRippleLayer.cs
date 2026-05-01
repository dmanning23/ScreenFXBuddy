using System;
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

    // Cached parameter handles — set once in LoadContent
    private EffectParameter _pDropletData;
    private EffectParameter _pDropletCount;
    private EffectParameter _pWaveSpeed;
    private EffectParameter _pWaveFrequency;
    private EffectParameter _pRefractionStrength;
    private EffectParameter _pReflectionStrength;
    private EffectParameter _pReflectionColor;
    private EffectParameter _pAspectRatio;

    // Reused each frame to avoid allocations
    private readonly Vector4[] _dropletBuffer = new Vector4[MaxInstances];

    /// <summary>
    /// The max number of ripples that can be displayed by the shader.
    /// Additional ripples are accepted but only the first MaxInstances are rendered.
    /// </summary>
    private const int MaxInstances = 16;

    /// <summary>How fast the wave pattern travels outward (UV units per normalized age unit).</summary>
    public float WaveSpeed { get; set; } = 1.0f;

    /// <summary>Controls ring spacing — higher values produce tighter, more numerous rings.</summary>
    public float WaveFrequency { get; set; } = 25f;

    /// <summary>UV displacement scale. 0.02 = subtle; 0.1 = very strong.</summary>
    public float RefractionStrength { get; set; } = 0.02f;

    /// <summary>How strongly the reflection color tints wave crests. 0 = no tint.</summary>
    public float ReflectionStrength { get; set; } = 0f;

    /// <summary>Color blended into wave crests when ReflectionStrength &gt; 0.</summary>
    public Vector3 ReflectionColor { get; set; } = Vector3.One;

    public bool IsActive => _ripples.Count > 0;

    public ForceRippleLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_Ripple");
        _pDropletData        = _effect.Parameters["DropletData"];
        _pDropletCount       = _effect.Parameters["DropletCount"];
        _pWaveSpeed          = _effect.Parameters["WaveSpeed"];
        _pWaveFrequency      = _effect.Parameters["WaveFrequency"];
        _pRefractionStrength = _effect.Parameters["RefractionStrength"];
        _pReflectionStrength = _effect.Parameters["ReflectionStrength"];
        _pReflectionColor    = _effect.Parameters["ReflectionColor"];
        _pAspectRatio        = _effect.Parameters["AspectRatio"];
    }

    /// <summary>
    /// Spawn a new ripple.
    /// </summary>
    /// <param name="position">UV-space origin [0,1] of the ripple center.</param>
    /// <param name="strength">Per-instance displacement multiplier (default 5).</param>
    /// <param name="speed">Passed through to RippleInstance for future use.</param>
    /// <param name="size">Passed through to RippleInstance for future use.</param>
    /// <param name="time">Lifetime in seconds.</param>
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
                _ripples.RemoveAt(i);
            else
                i++;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        int count = Math.Min(_ripples.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var r = _ripples[i];
            float age = 1f - (r.Timer.RemainingTime / r.TotalTime);
            _dropletBuffer[i] = new Vector4(r.Position.X, r.Position.Y, age, r.Strength);
        }

        float aspectRatio = (float)_graphicsDevice.Viewport.Width / _graphicsDevice.Viewport.Height;

        _pDropletData.SetValue(_dropletBuffer);
        _pDropletCount.SetValue((float)count);
        _pWaveSpeed.SetValue(WaveSpeed);
        _pWaveFrequency.SetValue(WaveFrequency);
        _pRefractionStrength.SetValue(RefractionStrength);
        _pReflectionStrength.SetValue(ReflectionStrength);
        _pReflectionColor.SetValue(ReflectionColor);
        _pAspectRatio.SetValue(aspectRatio);

        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
