using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class SpeedLinesLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Texture2D _whitePixel;
    private readonly List<SpeedLinesInstance> _instances = new();

    private EffectParameter _pCenter;
    private EffectParameter _pLineColor;
    private EffectParameter _pLineCount;
    private EffectParameter _pInnerRadius;
    private EffectParameter _pMaxRadius;
    private EffectParameter _pAspectRatio;   // REQUIRED — shader breaks without this

    public bool IsActive => _instances.Count > 0;

    public SpeedLinesLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Overlay_SpeedLines");
        _pCenter      = _effect.Parameters["Center"];
        _pLineColor   = _effect.Parameters["LineColor"];
        _pLineCount   = _effect.Parameters["LineCount"];
        _pInnerRadius = _effect.Parameters["InnerRadius"];
        _pMaxRadius   = _effect.Parameters["MaxRadius"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];

        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Vector2 pixelPosition, Color color,
        SpeedLinesMode linesMode = SpeedLinesMode.Expand,
        FadeMode fadeMode        = FadeMode.FadeOut,
        FadeCurve fadeCurve      = FadeCurve.Logarithmic,
        int lineCount            = 24,
        float maxRadius          = 1.0f,
        float duration           = 1f)
    {
        _instances.Add(new SpeedLinesInstance(
            pixelPosition, color, linesMode, fadeMode, fadeCurve, lineCount, maxRadius, duration));
    }

    public void Update(GameTime gameTime)
    {
        int i = 0;
        while (i < _instances.Count)
        {
            _instances[i].Update(gameTime);
            if (!_instances[i].IsAlive)
                _instances.RemoveAt(i);
            else
                i++;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        var vp = _graphicsDevice.Viewport;
        float aspectRatio = (float)vp.Width / vp.Height;

        foreach (var inst in _instances)
        {
            var uvCenter = new Vector2(
                inst.PixelPosition.X / vp.Width,
                inst.PixelPosition.Y / vp.Height);

            _pCenter.SetValue(uvCenter);
            _pLineColor.SetValue((inst.Color * inst.CurrentAlpha).ToVector4());
            _pLineCount.SetValue((float)inst.LineCount);
            _pInnerRadius.SetValue(inst.CurrentInnerRadius);
            _pMaxRadius.SetValue(inst.MaxRadius);
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
