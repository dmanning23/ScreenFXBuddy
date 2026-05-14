using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class SmokeLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private readonly List<SmokeInstance> _instances = new();

    private EffectParameter _pOrigin;
    private EffectParameter _pSmokeColor;
    private EffectParameter _pRadius;
    private EffectParameter _pProgress;
    private EffectParameter _pTime;
    private EffectParameter _pAspectRatio;

    private float _time;

    private const int MaxInstances = 4;

    private record struct SmokeInstance(Vector2 Position, Vector4 Color, float Radius, float Duration, float Age);

    public bool IsActive => _instances.Count > 0;

    public SmokeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Overlay_Smoke");
        _pOrigin      = _effect.Parameters["Origin"];
        _pSmokeColor  = _effect.Parameters["SmokeColor"];
        _pRadius      = _effect.Parameters["Radius"];
        _pProgress    = _effect.Parameters["Progress"];
        _pTime        = _effect.Parameters["Time"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Vector2 position, Color color, float radius = 0.15f, float duration = 2.0f)
    {
        if (_instances.Count >= MaxInstances)
            return;

        _instances.Add(new SmokeInstance(position, color.ToVector4(), radius, duration, 0f));
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

    public void Apply(SpriteBatch spriteBatch)
    {
        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        foreach (var inst in _instances)
        {
            var uvOrigin = new Vector2(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height);

            float progress = MathHelper.Clamp(inst.Age / inst.Duration, 0f, 1f);

            _pOrigin.SetValue(uvOrigin);
            _pSmokeColor.SetValue(inst.Color);
            _pRadius.SetValue(inst.Radius);
            _pProgress.SetValue(progress);
            _pTime.SetValue(_time);
            _pAspectRatio.SetValue(aspectRatio);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                _effect);
            spriteBatch.Draw(_whitePixel, vp.Bounds, Color.White);
            spriteBatch.End();
        }
    }

    public void Dispose()
    {
        _effect?.Dispose();
        _effect = null;
        _whitePixel?.Dispose();
        _whitePixel = null;
    }
}
