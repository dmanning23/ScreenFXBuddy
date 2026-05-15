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

    private Vector2 Position { get; set; }
    private float Strength { get; set; }
    private int NumCells{ get; set; }
    private float Seed { get; set; }

    private CountdownTimer Timer { get; set; } = new CountdownTimer();

    public bool IsActive => !Timer.Paused && Timer.HasTimeRemaining;

    public GlassShatterLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_GlassShatter");
        _pOrigin = _effect.Parameters["Origin"];
        _pStrength = _effect.Parameters["Strength"];
        _pNumCells = _effect.Parameters["NumCells"];
        _pSeed = _effect.Parameters["Seed"];
        _pShatter = _effect.Parameters["Shatter"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
    }

    /// <param name="position">Pixel-space position of the impact point.</param>
    /// <param name="strength">Peak UV displacement per cell. 0.04 is subtle; 0.10 is dramatic.</param>
    /// <param name="numCells">Number of Voronoi cells (shatter fragments).</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(Vector2 position, float strength = 0.04f, int numCells = 20, float duration = 0.8f)
    {
        if (numCells < 2) numCells = 2;
        Seed = Random.Shared.NextSingle() * 1000f;
        Position = position;
        Strength = strength;
        NumCells = numCells;
        Timer.Start(duration);
    }

    public void Update(GameClock clock)
    {
        Timer.Update(clock);
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!IsActive)
        {
            return;
        }

        float shatter = (float)Math.Sin(Timer.Lerp * Math.PI);

        var vp = _graphicsDevice.Viewport;
        var originUV = new Vector2(Position.X / vp.Width, Position.Y / vp.Height);

        _graphicsDevice.SetRenderTarget(destination);

        _pOrigin.SetValue(originUV);
        _pStrength.SetValue(Strength);
        _pNumCells.SetValue((float)NumCells);
        _pSeed.SetValue(Seed);
        _pShatter.SetValue(shatter);
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
