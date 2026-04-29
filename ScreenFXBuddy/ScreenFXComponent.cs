using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ScreenFXBuddy.Effects;

namespace ScreenFXBuddy;

public class ScreenFXComponent : DrawableGameComponent
{
    private SpriteBatch _spriteBatch;
    private RenderTarget2D _sceneTarget;
    private RenderTarget2D _pingTarget;
    private RenderTarget2D _pongTarget;

    public List<IDistortionLayer> DistortionLayers { get; } = new();
    public List<IOverlayLayer> OverlayLayers { get; } = new();

    public ForceRippleLayer ForceRipple { get; private set; }
    public GravityWaveLayer GravityWave { get; private set; }
    public ScreenShakeLayer ScreenShake { get; private set; }
    public ChromaticAberrationLayer ChromaticAberration { get; private set; }
    public HeatHazeLayer HeatHaze { get; private set; }
    public HitFlashLayer HitFlash { get; private set; }
    public AnimeSuperLayer AnimeSuper { get; private set; }

    public ScreenFXComponent(Game game) : base(game) { }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var pp = GraphicsDevice.PresentationParameters;
        _sceneTarget = CreateTarget(pp);
        _pingTarget  = CreateTarget(pp);
        _pongTarget  = CreateTarget(pp);

        ForceRipple          = new ForceRippleLayer(GraphicsDevice);
        GravityWave          = new GravityWaveLayer(GraphicsDevice);
        ScreenShake          = new ScreenShakeLayer(GraphicsDevice);
        ChromaticAberration  = new ChromaticAberrationLayer(GraphicsDevice);
        HeatHaze             = new HeatHazeLayer(GraphicsDevice);
        HitFlash             = new HitFlashLayer(GraphicsDevice);
        AnimeSuper           = new AnimeSuperLayer(GraphicsDevice);

        DistortionLayers.AddRange(new IDistortionLayer[]
            { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze });
        OverlayLayers.AddRange(new IOverlayLayer[]
            { HitFlash, AnimeSuper });

        foreach (var layer in DistortionLayers) layer.LoadContent(Game.Content);
        foreach (var layer in OverlayLayers)    layer.LoadContent(Game.Content);
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var layer in DistortionLayers) layer.Update(gameTime);
        foreach (var layer in OverlayLayers)    layer.Update(gameTime);
        base.Update(gameTime);
    }

    public void BeginCapture()
    {
        GraphicsDevice.SetRenderTarget(_sceneTarget);
    }

    public void EndCapture()
    {
        // Ping-pong through distortion layers.
        // source starts at _sceneTarget; we alternate writing to _pingTarget / _pongTarget.
        var source  = _sceneTarget;
        bool usePing = true;

        foreach (var layer in DistortionLayers)
        {
            if (!layer.IsActive) continue;
            var dest = usePing ? _pingTarget : _pongTarget;
            layer.Apply(_spriteBatch, source, dest);
            source  = dest;
            usePing = !usePing;
        }

        // Blit final result (or unmodified scene) to back buffer.
        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        _spriteBatch.Draw(source, GraphicsDevice.Viewport.Bounds, Color.White);
        _spriteBatch.End();

        // Additive overlay layers on top of the back buffer.
        foreach (var layer in OverlayLayers)
        {
            if (!layer.IsActive) continue;
            layer.Apply(_spriteBatch);
        }
    }

    public void TriggerForceRipple(Vector2 position, float strength = 1f)
        => ForceRipple.Trigger(position, strength);

    public void TriggerGravityWave(Vector2 position, float strength = 1f)
        => GravityWave.Trigger(position, strength);

    public void TriggerScreenShake(float trauma)
        => ScreenShake.Trigger(trauma);

    public void TriggerChromaticAberration(float intensity, float duration)
        => ChromaticAberration.Trigger(intensity, duration);

    public void TriggerHeatHaze(float intensity, float duration)
        => HeatHaze.Trigger(intensity, duration);

    public void TriggerHitFlash(Color color, float duration)
        => HitFlash.Trigger(color, duration);

    public void TriggerAnimeSuper(Color color, float duration)
        => AnimeSuper.Trigger(color, duration);

    private RenderTarget2D CreateTarget(PresentationParameters pp) =>
        new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight,
            false, pp.BackBufferFormat, pp.DepthStencilFormat);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sceneTarget?.Dispose();
            _pingTarget?.Dispose();
            _pongTarget?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
