using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public class FreezeFrameLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pTintColor;
    private EffectParameter _pIntensity;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private Vector4 _tintColor;
    private float   _flashIn;
    private float   _hold;
    private float   _fadeOut;
    private float   _age;
    private bool    _active;

    public bool IsActive => _active;

    public FreezeFrameLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect        = content.Load<Effect>("Distorter_FreezeFrame");
        _pTintColor    = _effect.Parameters["TintColor"];
        _pIntensity    = _effect.Parameters["Intensity"];
        _pAspectRatio  = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="tintColor">Color to tint the desaturated scene toward. (100,160,255) = icy blue.</param>
    /// <param name="flashIn">Seconds to ramp from no effect to full effect.</param>
    /// <param name="hold">Seconds at full effect intensity.</param>
    /// <param name="fadeOut">Seconds to fade back to normal.</param>
    public void Trigger(Color tintColor, float flashIn = 0.10f, float hold = 0.40f, float fadeOut = 0.30f)
    {
        _tintColor = tintColor.ToVector4();
        _flashIn   = flashIn;
        _hold      = hold;
        _fadeOut   = fadeOut;
        _age       = 0f;
        _active    = true;
    }

    public void Update(GameClock clock)
    {
        if (!_active) return;
        _age += clock.TimeDelta;
        if (_age >= _flashIn + _hold + _fadeOut)
            _active = false;
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!_active) return;

        float intensity;
        if (_age < _flashIn)
            intensity = _flashIn > 0f ? _age / _flashIn : 1f;
        else if (_age < _flashIn + _hold)
            intensity = 1f;
        else
        {
            float fadeProgress = _age - _flashIn - _hold;
            intensity = _fadeOut > 0f ? 1f - fadeProgress / _fadeOut : 0f;
        }

        var vp = _graphicsDevice.Viewport;
        _graphicsDevice.SetRenderTarget(destination);

        _pTintColor.SetValue(_tintColor);
        _pIntensity.SetValue(intensity);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);
        _pSceneTexture.SetValue(source);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
