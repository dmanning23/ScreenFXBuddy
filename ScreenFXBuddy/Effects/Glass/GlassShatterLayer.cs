using System;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class GlassShatterLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pOrigin;
    private EffectParameter _pStrength;
    private EffectParameter _pNumCells;
    private EffectParameter _pSeed;
    private EffectParameter _pShatter;
    private EffectParameter _pAspectRatio;

    private record struct ShatterInstance(
        Vector2 Position,
        float Strength,
        int NumCells,
        float Seed,
        float Duration,
        float Age);

    private ShatterInstance? _instance;

    public bool IsActive => _instance.HasValue;

    public GlassShatterLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect       = content.Load<Effect>("Distorter_GlassShatter");
        _pOrigin      = _effect.Parameters["Origin"];
        _pStrength    = _effect.Parameters["Strength"];
        _pNumCells    = _effect.Parameters["NumCells"];
        _pSeed        = _effect.Parameters["Seed"];
        _pShatter     = _effect.Parameters["Shatter"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
    }

    /// <param name="position">Pixel-space position of the impact point.</param>
    /// <param name="strength">Peak UV displacement per cell. 0.04 is subtle; 0.10 is dramatic.</param>
    /// <param name="numCells">Number of Voronoi cells (shatter fragments).</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.04f, int numCells = 20, float duration = 0.8f)
    {
        if (numCells < 2) numCells = 2;
        float seed = Random.Shared.NextSingle() * 1000f;
        _instance = new ShatterInstance(position, strength, numCells, seed, duration, 0f);
    }

    public void Update(GameClock clock)
    {
        if (!_instance.HasValue) return;
        var inst = _instance.Value;
        inst = inst with { Age = inst.Age + clock.TimeDelta };
        _instance = inst.Age >= inst.Duration ? null : inst;
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!_instance.HasValue) return;
        var inst = _instance.Value;

        float t       = inst.Age / inst.Duration;
        float shatter = (float)Math.Sin(t * Math.PI);

        var vp        = _graphicsDevice.Viewport;
        var originUV  = new Vector2(inst.Position.X / vp.Width, inst.Position.Y / vp.Height);

        _graphicsDevice.SetRenderTarget(destination);

        _pOrigin.SetValue(originUV);
        _pStrength.SetValue(inst.Strength);
        _pNumCells.SetValue((float)inst.NumCells);
        _pSeed.SetValue(inst.Seed);
        _pShatter.SetValue(shatter);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
