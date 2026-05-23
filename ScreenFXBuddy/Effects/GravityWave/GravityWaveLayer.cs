using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class GravityWaveLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pWaveCount;
    private EffectParameter _pWaveOrigins;
    private EffectParameter _pWaveState;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pBandWidth;

    private readonly List<GravityWaveInstance> _instances = new();

    private const int MaxInstances = 8;
    private const float BandWidth = 0.06f;

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer = new Vector4[MaxInstances];

    public bool IsActive => _instances.Count > 0;

    public GravityWaveLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_GravityWave");
        _pWaveCount = _effect.Parameters["WaveCount"];
        _pWaveOrigins = _effect.Parameters["WaveOrigins"];
        _pWaveState = _effect.Parameters["WaveState"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pBandWidth = _effect.Parameters["BandWidth"];
    }

    public void Trigger(
        Vector2 position,
        float strength = 0.04f,
        float startHeight = 0.05f,
        float endHeight = 0.25f,
        float speed = 0.5f,
        float duration = 1.5f)
    {
        _instances.Add(new GravityWaveInstance(position, strength, startHeight, endHeight, speed, duration));
    }

    public void Update(GameClock clock)
    {
        var i = 0;
        while (i < _instances.Count)
        {
            _instances[i].Update(clock);
            if (!_instances[i].IsAlive)
                _instances.RemoveAt(i);
            else
                i++;
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!IsActive)
        {
            return;
        }

        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        int count = Math.Min(_instances.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var inst = _instances[i];

            _originBuffer[i] = new Vector4(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height,
                0f, 0f);

            _stateBuffer[i] = new Vector4(
                inst.Timer.CurrentTime * inst.Speed,
                inst.Timer.LerpValues(inst.StartHeight, inst.EndHeight),
                inst.Strength * inst.Timer.Lerp,
                0f);
        }

        _pWaveCount.SetValue((float)count);
        _pWaveOrigins.SetValue(_originBuffer);
        _pWaveState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);
        _pBandWidth.SetValue(BandWidth);

        _graphicsDevice.SetRenderTarget(destination);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
