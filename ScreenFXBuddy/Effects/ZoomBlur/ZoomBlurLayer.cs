using System;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ScreenFXBuddy.Effects;

public class ZoomBlurLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private readonly Vector4[] _originBuffer = new Vector4[MaxInstances];
    private readonly Vector4[] _stateBuffer = new Vector4[MaxInstances];
    private EffectParameter _pInstanceCount;
    private EffectParameter _pOrigins;
    private EffectParameter _pStates;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private readonly List<ZoomBlurInstance> _instances = new();

    private const int MaxInstances = 8;

    public bool IsActive => _instances.Count > 0;

    public ZoomBlurLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_ZoomBlur");
        _pInstanceCount = _effect.Parameters["InstanceCount"];
        _pOrigins = _effect.Parameters["Origins"];
        _pStates = _effect.Parameters["States"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="position">Pixel-space position the blur radiates from.</param>
    /// <param name="peakStrength">Peak UV displacement. 0.05 is subtle; 0.15 is dramatic.</param>
    /// <param name="radius">UV-space radius of affected area. 1.0 = full screen.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float peakStrength = 0.05f, float radius = 1.0f, float duration = 0.4f)
    {
        _instances.Add(new ZoomBlurInstance(position, peakStrength, radius, duration));
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

        int count = Math.Min(_instances.Count, MaxInstances);
        for (int i = 0; i < count; i++)
        {
            var inst = _instances[i];

            float currentStrength = inst.PeakStrength * MathF.Sin(inst.Timer.Lerp * MathF.PI);
            var originUV = new Vector2(inst.Position.X / vp.Width, inst.Position.Y / vp.Height);

            _originBuffer[i] = new Vector4(
                originUV.X,
                originUV.Y,
                0f, 0f);

            _stateBuffer[i] = new Vector4(
                currentStrength,
                inst.Radius,
                0f, 0f);
        }
        _graphicsDevice.SetRenderTarget(destination);

        _pInstanceCount.SetValue((float)count);
        _pOrigins.SetValue(_originBuffer);
        _pStates.SetValue(_stateBuffer);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);
        _pSceneTexture.SetValue(source);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
