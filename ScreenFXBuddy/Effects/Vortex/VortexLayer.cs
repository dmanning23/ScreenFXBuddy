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
    private readonly Vector4[] _stateBuffer = new Vector4[MaxInstances];

    public bool IsActive => _instances.Count > 0;

    public VortexLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_Vortex");
        _pVortexCount = _effect.Parameters["VortexCount"];
        _pVortexOrigins = _effect.Parameters["VortexOrigins"];
        _pVortexState = _effect.Parameters["VortexState"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
    }

    /// <param name="position">Pixel-space position of the vortex centre.</param>
    /// <param name="strength">Peak swirl magnitude. 0.30 is visible; higher values are dramatic.</param>
    /// <param name="radius">UV-space radius of the affected area. 0.25 covers roughly a quarter of screen height.</param>
    /// <param name="speed">Rotation direction and speed multiplier. Positive = clockwise; negative = counter-clockwise.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(
        Vector2 position,
        float radius = 0.25f,
        float speed = 1f,
        float spinInTime = 0.3f,
        float spinOutTime = 0.3f,
        FadeCurve fadeCurve = FadeCurve.Linear,
        bool clockwise = true)
    {
        _instances.Add(new VortexInstance(position, radius, speed, spinInTime, spinOutTime, fadeCurve, clockwise));
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

        _graphicsDevice.SetRenderTarget(destination);

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

            _stateBuffer[i] = new Vector4(inst.SwirlAmount(), inst.Radius, 0f, 0f);
        }

        _pVortexCount.SetValue((float)count);
        _pVortexOrigins.SetValue(_originBuffer);
        _pVortexState.SetValue(_stateBuffer);
        _pAspectRatio.SetValue(aspectRatio);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
