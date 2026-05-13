using System;
using System.Collections.Generic;
using System.Linq;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ScreenFXBuddy.Effects;

namespace ScreenFXBuddy;

public class ScreenFXComponent : IScreenFXService, IDisposable
{
    private Game Game { get; set; }
    private GraphicsDevice GraphicsDevice { get; set; }
    private SpriteBatch _spriteBatch;
    private RenderTarget2D _sceneTarget;
    private RenderTarget2D _pingTarget;
    private RenderTarget2D _pongTarget;
    private bool _capturing;
    private Point _renderTargetSize;

    public List<IDistortionLayer> DistortionLayers { get; } = new();
    public List<IOverlayLayer> OverlayLayers { get; } = new();

    public ForceRippleLayer ForceRipple { get; private set; }
    public GravityWaveLayer GravityWave { get; private set; }
    public ScreenShakeLayer ScreenShake { get; private set; }
    public ChromaticAberrationLayer ChromaticAberration { get; private set; }
    public HeatHazeLayer HeatHaze { get; private set; }
    public HitFlashLayer HitFlash { get; private set; }
    public AnimeSuperLayer AnimeSuper { get; private set; }
    public SpeedLinesLayer SpeedLines { get; private set; }

    private ContentManager Content { get; set; }

    public ScreenFXComponent(Game game)
    {
        Game = game;
        Game.Services.AddService<IScreenFXService>(this);
    }

    public void LoadContent(ContentManager contentManager = null)
    {
        GraphicsDevice = Game.GraphicsDevice;
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var pp = GraphicsDevice.PresentationParameters;
        _renderTargetSize = new Point(pp.BackBufferWidth, pp.BackBufferHeight);
        _sceneTarget = CreateTarget(_renderTargetSize);
        _pingTarget = CreateTarget(_renderTargetSize);
        _pongTarget = CreateTarget(_renderTargetSize);

        ForceRipple = new ForceRippleLayer(GraphicsDevice);
        GravityWave = new GravityWaveLayer(GraphicsDevice);
        ScreenShake = new ScreenShakeLayer(GraphicsDevice);
        ChromaticAberration = new ChromaticAberrationLayer(GraphicsDevice);
        HeatHaze = new HeatHazeLayer(GraphicsDevice);
        HitFlash = new HitFlashLayer(GraphicsDevice);
        AnimeSuper = new AnimeSuperLayer(GraphicsDevice);
        SpeedLines = new SpeedLinesLayer(GraphicsDevice);

        DistortionLayers.AddRange(new IDistortionLayer[]
            { ForceRipple, GravityWave, ScreenShake, ChromaticAberration, HeatHaze });
        OverlayLayers.AddRange(new IOverlayLayer[]
            { HitFlash, AnimeSuper, SpeedLines });

        //Let's use our own content manager
        if (null == contentManager)
        {
            Content = new ContentManager(Game.Services)
            {
                RootDirectory = "Content"
            };
        }
        else
        {
            Content = contentManager;
        }

        foreach (var layer in DistortionLayers) layer.LoadContent(Content);
        foreach (var layer in OverlayLayers) layer.LoadContent(Content);
    }

    public void Update(GameClock clock)
    {
        foreach (var layer in DistortionLayers) layer.Update(clock);
        foreach (var layer in OverlayLayers) layer.Update(clock);
    }

    public void BeginCapture(Point? virtualResolution = null)
    {
        var size = virtualResolution ?? _renderTargetSize;
        if (size != _renderTargetSize)
        {
            _renderTargetSize = size;
            _sceneTarget?.Dispose();
            _pingTarget?.Dispose();
            _pongTarget?.Dispose();
            _sceneTarget = CreateTarget(_renderTargetSize);
            _pingTarget = CreateTarget(_renderTargetSize);
            _pongTarget = CreateTarget(_renderTargetSize);
        }

        _capturing = true;
        GraphicsDevice.SetRenderTarget(_sceneTarget);
    }

    public void EndCapture(Matrix? transformMatrix = null, Action resetViewport = null)
    {
        if (!_capturing) return;
        _capturing = false;

        // Ping-pong through distortion layers.
        // source starts at _sceneTarget; we alternate writing to _pingTarget / _pongTarget.
        var source = _sceneTarget;
        bool usePing = true;

        foreach (var layer in DistortionLayers)
        {
            if (!layer.IsActive) continue;
            var dest = usePing ? _pingTarget : _pongTarget;
            layer.Apply(_spriteBatch, source, dest);
            source = dest;
            usePing = !usePing;
        }

        // Blit final result (or unmodified scene) to back buffer.
        GraphicsDevice.SetRenderTarget(null);
        resetViewport?.Invoke();

        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            null, transformMatrix ?? Matrix.Identity);

        if (transformMatrix.HasValue)
            _spriteBatch.Draw(source, Vector2.Zero, Color.White);
        else
            _spriteBatch.Draw(source, GraphicsDevice.Viewport.Bounds, Color.White);

        _spriteBatch.End();

        // Additive overlay layers on top of the back buffer.
        foreach (var layer in OverlayLayers)
        {
            if (!layer.IsActive) continue;
            layer.Apply(_spriteBatch);
        }
    }

    public void TriggerForceRipple(Vector2 position, float strength = 0.05f, float speed = 0.4f, float size = 0.08f, float time = 2f)
        => ForceRipple.Trigger(position, strength, speed, size, time);

    public void TriggerGravityWave(
        Vector2 position,
        float strength = 0.04f,
        float startHeight = 0.05f,
        float endHeight = 0.25f,
        float speed = 0.5f,
        float duration = 1.5f)
        => GravityWave.Trigger(position, strength, startHeight, endHeight, speed, duration);

    public void TriggerScreenShake(float length = 1f, float delta = 0.1f, float amount = 0.1f)
        => ScreenShake.Trigger(length, delta, amount);

    public void TriggerChromaticAberration(Vector2 startPosition,
        float distance = 0.1f,
        float time = 2f,
        FadeCurve curve = FadeCurve.Linear)
        => ChromaticAberration.Trigger(startPosition, distance, time, curve);

    public void TriggerHeatHaze(float intensity, float duration)
        => HeatHaze.Trigger(intensity, duration);

    public void TriggerHitFlash(Color blendColor,
        FadeMode mode = FadeMode.FadeOut,
        FadeCurve curve = FadeCurve.Linear,
        EffectBlendMode blendMode = EffectBlendMode.LinearDodge,
        float time = 1f)
        => HitFlash.Trigger(blendColor, mode, curve, blendMode, time);

    public void TriggerAnimeSuper(Color color, float duration)
        => AnimeSuper.Trigger(color, duration);

    public void TriggerSpeedLines(
        Vector2 position,
        Color color,
        SpeedLinesMode linesMode = SpeedLinesMode.Expand,
        FadeMode fadeMode = FadeMode.FadeOut,
        FadeCurve fadeCurve = FadeCurve.Logarithmic,
        int lineCount = 24,
        float maxRadius = 1.0f,
        float duration = 1f)
        => SpeedLines.Trigger(position, color, linesMode, fadeMode, fadeCurve, lineCount, maxRadius, duration);

    private RenderTarget2D CreateTarget(Point size)
    {
        var pp = GraphicsDevice.PresentationParameters;
        return new RenderTarget2D(GraphicsDevice, size.X, size.Y,
            false, pp.BackBufferFormat, pp.DepthStencilFormat);
    }

    public void Dispose()
    {
        _sceneTarget?.Dispose();
        _pingTarget?.Dispose();
        _pongTarget?.Dispose();
        _spriteBatch?.Dispose();

        foreach (var layer in DistortionLayers.OfType<IDisposable>()) layer.Dispose();
        DistortionLayers.Clear();
        foreach (var layer in OverlayLayers.OfType<IDisposable>()) layer.Dispose();
        OverlayLayers.Clear();
    }
}
