using System;
using System.Collections.Generic;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ElectricLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;

    private EffectParameter _pOrigin;
    private EffectParameter _pElecColor;
    private EffectParameter _pRadius;
    private EffectParameter _pProgress;
    private EffectParameter _pTime;
    private EffectParameter _pAspectRatio;

    private const int MaxInstances = 4;
    private readonly List<ElectricInstance> _instances = new();

    public bool IsActive => _instances.Count > 0;

    public ElectricLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Overlay_Electric");
        _pOrigin = _effect.Parameters["Origin"];
        _pElecColor = _effect.Parameters["ElecColor"];
        _pRadius = _effect.Parameters["Radius"];
        _pProgress = _effect.Parameters["Progress"];
        _pTime = _effect.Parameters["Time"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <param name="position">Pixel-space emitter position.</param>
    /// <param name="color">Electricity tint. (150,200,255) = blue-white; (180,80,255) = purple.</param>
    /// <param name="radius">Max spread radius in UV units. 0.20 = roughly a quarter-screen circle.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, Color color, float radius = 0.20f, float duration = 0.50f)
    {
        _instances.Add(new ElectricInstance(position, color, radius, duration));
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
        if (_instances.Count == 0) return;

        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        foreach (var inst in _instances)
        {
            float progress = inst.Timer.Lerp;
            var uvOrigin = new Vector2(inst.Position.X / vp.Width, inst.Position.Y / vp.Height);

            _pOrigin.SetValue(uvOrigin);
            _pElecColor.SetValue(inst.Color.ToVector4());
            _pRadius.SetValue(inst.Radius);
            _pProgress.SetValue(progress);
            _pTime.SetValue(inst.Timer.CurrentTime);
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
        _whitePixel?.Dispose();
    }
}
