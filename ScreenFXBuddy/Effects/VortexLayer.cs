using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class VortexLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pVortexCount;
    private EffectParameter _pVortexOrigins;
    private EffectParameter _pVortexState;
    private EffectParameter _pAspectRatio;

    private readonly List<VortexInstance> _instances = new();

    private const int MaxInstances = 4;

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];

    public bool IsActive => _instances.Count > 0;

    public VortexLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_Vortex");
        _pVortexCount  = _effect.Parameters["VortexCount"];
        _pVortexOrigins = _effect.Parameters["VortexOrigins"];
        _pVortexState  = _effect.Parameters["VortexState"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
    }

    /// <param name="position">Pixel-space position of the vortex centre.</param>
    /// <param name="strength">Peak swirl magnitude. 0.30 is visible; higher values are dramatic.</param>
    /// <param name="radius">UV-space radius of the affected area. 0.25 covers roughly a quarter of screen height.</param>
    /// <param name="speed">Rotation direction and speed multiplier. Positive = clockwise; negative = counter-clockwise.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(
        Vector2 position,
        float strength = 0.30f,
        float radius   = 0.25f,
        float speed    = 2.00f,
        float duration = 0.60f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new VortexInstance(position, strength, radius, speed, duration, 0f));
    }

    public void Update(GameClock clock)
    {
        float dt = clock.TimeDelta;
        for (int i = _instances.Count - 1; i >= 0; i--)
        {
            var inst = _instances[i];
            inst = inst with { Age = inst.Age + dt };
            if (inst.Age >= inst.Duration)
                _instances.RemoveAt(i);
            else
                _instances[i] = inst;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        _graphicsDevice.SetRenderTarget(destination);

        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        int count = Math.Min(_instances.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var inst = _instances[i];
            float t     = inst.Age / inst.Duration;
            float swirl = inst.Strength * inst.Speed * (1f - t);

            _originBuffer[i] = new Vector4(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height,
                0f, 0f);

            _stateBuffer[i] = new Vector4(swirl, inst.Radius, 0f, 0f);
        }

        _pVortexCount.SetValue((float)count);
        _pVortexOrigins.SetValue(_originBuffer);
        _pVortexState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }

    private record struct VortexInstance(
        Vector2 Position,
        float Strength,
        float Radius,
        float Speed,
        float Duration,
        float Age);
}
