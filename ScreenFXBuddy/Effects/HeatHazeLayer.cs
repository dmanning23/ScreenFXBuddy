using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class HeatHazeLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pHazeCount;
    private EffectParameter _pHazeOrigins;
    private EffectParameter _pHazeState;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pTime;

    private readonly List<HazeInstance> _instances = new();

    private const int MaxInstances = 8;

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer  = new Vector4[MaxInstances];

    private float _time;

    public bool IsActive => _instances.Count > 0;

    public HeatHazeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_HeatHaze");
        _pHazeCount   = _effect.Parameters["HazeCount"];
        _pHazeOrigins = _effect.Parameters["HazeOrigins"];
        _pHazeState   = _effect.Parameters["HazeState"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pTime        = _effect.Parameters["Time"];
    }

    public void Trigger(
        Vector2 position,
        float strength = 0.02f,
        float radius   = 0.15f,
        float height   = 0.40f,
        float duration = 3.0f)
    {
        if (_instances.Count >= MaxInstances) return;
        _instances.Add(new HazeInstance(position, strength, radius, height, duration, 0f));
    }

    public void Update(GameClock clock)
    {
        float dt = clock.TimeDelta;
        _time += dt;

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
        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        int count = Math.Min(_instances.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var inst = _instances[i];

            float originX = inst.Position.X / vp.Width;
            float originY = inst.Position.Y / vp.Height;

            _originBuffer[i] = new Vector4(originX, originY, 0f, 0f);

            _stateBuffer[i] = new Vector4(
                inst.Radius,
                inst.Height,
                inst.Strength * (1f - inst.Age / inst.Duration),
                0f);
        }

        _graphicsDevice.SetRenderTarget(destination);

        _pHazeCount.SetValue((float)count);
        _pHazeOrigins.SetValue(_originBuffer);
        _pHazeState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);
        _pTime.SetValue(_time);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }

    private record struct HazeInstance(
        Vector2 Position,
        float Strength,
        float Radius,
        float Height,
        float Duration,
        float Age);
}
