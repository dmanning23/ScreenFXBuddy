using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class HitFlashLayer : IOverlayLayer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _whitePixel;
    private readonly List<HitFlashInstance> _flashes = new();

    // Pure add: src + dst. Intensity is pre-baked into src RGB via Color * amount.
    private static readonly BlendState _linearDodgeBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    // Screen: src + dst * (1 - src). Lightens without blowing out.
    private static readonly BlendState _screenBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    // Multiply: dst * src.rgb + dst * (1 - src.a) = dst * lerp(1, flashColor, intensity).
    private static readonly BlendState _multiplyBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    // LinearBurn approximation: dst - src (subtractive darkening).
    private static readonly BlendState _linearBurnBlend = new()
    {
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    // ColorBurn approximation: dst * (1 - src). Darkens by the inverse of the flash color.
    private static readonly BlendState _colorBurnBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    // ColorDodge approximation: dst * (src + 1). Brightens, blowing out in bright areas.
    private static readonly BlendState _colorDodgeBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    public bool IsActive => _flashes.Count > 0;

    public HitFlashLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Trigger(Color blendColor,
        FadeMode mode = FadeMode.FadeOut,
        FadeCurve curve = FadeCurve.Linear,
        FlashBlendMode blendMode = FlashBlendMode.LinearDodge,
        float time = 1f)
    {
        _flashes.Add(new HitFlashInstance(blendColor, mode, curve, blendMode, time));
    }

    public void Update(GameTime gameTime)
    {
        var i = 0;
        while (i < _flashes.Count)
        {
            _flashes[i].Update(gameTime);
            if (!_flashes[i].IsAlive)
                _flashes.RemoveAt(i);
            else
                i++;
        }
    }

    public void Apply(SpriteBatch spriteBatch)
    {
        foreach (var flash in _flashes)
        {
            var color = flash.GetCurrentColor();
            var blendState = GetBlendState(flash.FlashBlendMode);

            spriteBatch.Begin(SpriteSortMode.Immediate, blendState,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(_whitePixel, _graphicsDevice.Viewport.Bounds, color);
            spriteBatch.End();
        }
    }

    private static BlendState GetBlendState(FlashBlendMode mode) => mode switch
    {
        FlashBlendMode.Multiply    => _multiplyBlend,
        FlashBlendMode.ColorBurn   => _colorBurnBlend,
        FlashBlendMode.LinearBurn  => _linearBurnBlend,
        FlashBlendMode.Screen      => _screenBlend,
        FlashBlendMode.ColorDodge  => _colorDodgeBlend,
        _                          => _linearDodgeBlend
    };

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }
}
