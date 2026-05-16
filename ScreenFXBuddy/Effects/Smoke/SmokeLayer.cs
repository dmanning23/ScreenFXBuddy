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

    private const int MaxInstances = 4;

    public bool IsActive => _instances.Count > 0;

    public SmokeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Overlay_Smoke");
        _pOrigin = _effect.Parameters["Origin"];
        _pSmokeColor = _effect.Parameters["SmokeColor"];
        _pRadius = _effect.Parameters["Radius"];
        _pProgress = _effect.Parameters["Progress"];
        _pTime = _effect.Parameters["Time"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Vector2 position, Color color, float radius = 0.15f, float duration = 2.0f)
    {
        _instances.Add(new SmokeInstance(position, color, radius, duration));
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

    public void Apply(SpriteBatch spriteBatch)
    {
        if (!IsActive)
        {
            return;
        }

        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        foreach (var inst in _instances)
        {
            var uvOrigin = new Vector2(
                inst.Position.X / vp.Width,
                inst.Position.Y / vp.Height);

            float progress = MathHelper.Clamp(1f - inst.Timer.Lerp, 0f, 1f);

            _pOrigin.SetValue(uvOrigin);
            _pSmokeColor.SetValue(inst.Color.ToVector4());
            _pRadius.SetValue(inst.Radius);
            _pProgress.SetValue(progress);
            _pTime.SetValue(inst.Timer.CurrentTime);
            _pAspectRatio.SetValue(aspectRatio);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
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
