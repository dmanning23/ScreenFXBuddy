using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ScreenFXBuddy;
using ScreenFXBuddy.Effects;

namespace ScreenFXBuddy.Example;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private ScreenFXComponent _screenFX;
    private KeyboardState _prevKeys;

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _background = null!;

    //Window Size
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _screenFX = new ScreenFXComponent(this);
        Components.Add(_screenFX);
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _background = Content.Load<Texture2D>("Braid_screenshot8");

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (keys.IsKeyDown(Keys.Escape)) Exit();

        var center = new Vector2(0.5f, 0.5f);
        var centerPixels = new Vector2(1280 / 2, 720 / 2);

        //Test all the effects to make sure they are plumbed correctly
        if (keys.IsKeyDown(Keys.D1) && !_prevKeys.IsKeyDown(Keys.D1))
            _screenFX.TriggerForceRipple(centerPixels);
        if (keys.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2))
            _screenFX.TriggerGravityWave(center);
        if (keys.IsKeyDown(Keys.D3) && !_prevKeys.IsKeyDown(Keys.D3))
            _screenFX.TriggerScreenShake();
        if (keys.IsKeyDown(Keys.D4) && !_prevKeys.IsKeyDown(Keys.D4))
            _screenFX.TriggerChromaticAberration(centerPixels, 1f, 2f);
        if (keys.IsKeyDown(Keys.D5) && !_prevKeys.IsKeyDown(Keys.D5))
            _screenFX.TriggerHeatHaze(1f, 2f);
        if (keys.IsKeyDown(Keys.D6) && !_prevKeys.IsKeyDown(Keys.D6))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.LinearDodge, 2f);
        if (keys.IsKeyDown(Keys.D7) && !_prevKeys.IsKeyDown(Keys.D7))
            _screenFX.TriggerAnimeSuper(Color.White, 1f);

        // Basic expand + fade-out (default)
        if (keys.IsKeyDown(Keys.D8) && !_prevKeys.IsKeyDown(Keys.D8))
            _screenFX.TriggerSpeedLines(centerPixels, Color.White);

        // Static lines, fade-in, linear curve
        if (keys.IsKeyDown(Keys.D9) && !_prevKeys.IsKeyDown(Keys.D9))
            _screenFX.TriggerSpeedLines(centerPixels, Color.Yellow,
                SpeedLinesMode.Static, FadeMode.FadeIn, FadeCurve.Linear, 32, 1.0f, 1.5f);

        // Off-center burst, tight radius, many lines
        if (keys.IsKeyDown(Keys.D0) && !_prevKeys.IsKeyDown(Keys.D0))
            _screenFX.TriggerSpeedLines(new Vector2(300, 200), Color.Cyan,
                SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Exponential, 48, 0.6f, 0.8f);

        //Test several different variations of screen shake
        if (keys.IsKeyDown(Keys.Q) && !_prevKeys.IsKeyDown(Keys.Q))
            _screenFX.TriggerScreenShake(2f, 0.5f, 0.3f);
        if (keys.IsKeyDown(Keys.W) && !_prevKeys.IsKeyDown(Keys.W))
            _screenFX.TriggerScreenShake(0.5f, 0.1f, 0.05f);
        if (keys.IsKeyDown(Keys.E) && !_prevKeys.IsKeyDown(Keys.E))
            _screenFX.TriggerScreenShake(1f, 1f, 0.6f);
        if (keys.IsKeyDown(Keys.R) && !_prevKeys.IsKeyDown(Keys.R))
            _screenFX.TriggerScreenShake(0.1f, 0.05f, 0.1f);

        //Test different versions of chromatic aberration
        if (keys.IsKeyDown(Keys.T) && !_prevKeys.IsKeyDown(Keys.T))
            _screenFX.TriggerChromaticAberration(centerPixels, 0.5f, 2f);
        if (keys.IsKeyDown(Keys.Y) && !_prevKeys.IsKeyDown(Keys.Y))
            _screenFX.TriggerChromaticAberration(centerPixels, 2f, 0.5f);
        if (keys.IsKeyDown(Keys.U) && !_prevKeys.IsKeyDown(Keys.U))
            _screenFX.TriggerChromaticAberration(centerPixels, 4f, 2f);
        if (keys.IsKeyDown(Keys.I) && !_prevKeys.IsKeyDown(Keys.I))
            _screenFX.TriggerChromaticAberration(new Vector2(320, 360), 1f, 0.4f, FadeCurve.Exponential);   // left-of-center
        if (keys.IsKeyDown(Keys.O) && !_prevKeys.IsKeyDown(Keys.O))
            _screenFX.TriggerChromaticAberration(new Vector2(960, 360), 1f, 0.6f, FadeCurve.Logarithmic);   // right-of-center
        if (keys.IsKeyDown(Keys.P) && !_prevKeys.IsKeyDown(Keys.P))
            _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f);
        if (keys.IsKeyDown(Keys.OemOpenBrackets) && !_prevKeys.IsKeyDown(Keys.OemOpenBrackets))
            _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f, FadeCurve.Logarithmic);   // right-of-center
        if (keys.IsKeyDown(Keys.OemCloseBrackets) && !_prevKeys.IsKeyDown(Keys.OemCloseBrackets))
            _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f, FadeCurve.Exponential);   // right-of-center


        //Test several different ripple effetcs
        if (keys.IsKeyDown(Keys.A) && !_prevKeys.IsKeyDown(Keys.A))
            _screenFX.TriggerForceRipple(new Vector2(256, 256), 0.1f, 0.4f, 0.08f, 2f);
        if (keys.IsKeyDown(Keys.S) && !_prevKeys.IsKeyDown(Keys.S))
            _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 256), 0.05f, 0.8f, 0.08f, 2f);
        if (keys.IsKeyDown(Keys.D) && !_prevKeys.IsKeyDown(Keys.D))
            _screenFX.TriggerForceRipple(new Vector2(256, 720 - 256), 0.05f, 0.4f, 0.16f, 2f);
        if (keys.IsKeyDown(Keys.F) && !_prevKeys.IsKeyDown(Keys.F))
            _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 720 - 256), 0.05f, 0.4f, 0.08f, 4f);
        if (keys.IsKeyDown(Keys.G) && !_prevKeys.IsKeyDown(Keys.G))
            _screenFX.TriggerForceRipple(new Vector2(256, 256), 0.025f, 0.4f, 0.08f, 2f);
        if (keys.IsKeyDown(Keys.H) && !_prevKeys.IsKeyDown(Keys.H))
            _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 256), 0.05f, 0.2f, 0.08f, 2f);
        if (keys.IsKeyDown(Keys.J) && !_prevKeys.IsKeyDown(Keys.J))
            _screenFX.TriggerForceRipple(new Vector2(256, 720 - 256), 0.05f, 0.4f, 0.04f, 2f);
        if (keys.IsKeyDown(Keys.K) && !_prevKeys.IsKeyDown(Keys.K))
            _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 720 - 256), 0.05f, 0.4f, 0.08f, 1f);

        //Test several types of hit flash
        if (keys.IsKeyDown(Keys.Z) && !_prevKeys.IsKeyDown(Keys.Z))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Logarithmic, EffectBlendMode.LinearDodge, 2f);
        if (keys.IsKeyDown(Keys.X) && !_prevKeys.IsKeyDown(Keys.X))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Exponential, EffectBlendMode.LinearDodge, 2f);
        if (keys.IsKeyDown(Keys.C) && !_prevKeys.IsKeyDown(Keys.C))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.ColorBurn, 2f);
        if (keys.IsKeyDown(Keys.V) && !_prevKeys.IsKeyDown(Keys.V))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.ColorDodge, 2f);
        if (keys.IsKeyDown(Keys.B) && !_prevKeys.IsKeyDown(Keys.B))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.LinearBurn, 2f);
        if (keys.IsKeyDown(Keys.N) && !_prevKeys.IsKeyDown(Keys.N))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.Multiply, 2f);
        if (keys.IsKeyDown(Keys.M) && !_prevKeys.IsKeyDown(Keys.M))
            _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.Screen, 2f);

        _prevKeys = keys;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _screenFX.BeginCapture();

        GraphicsDevice.Clear(Color.CornflowerBlue);

        //draw game scene here
        _spriteBatch.Begin();
        _spriteBatch.Draw(_background, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
        _spriteBatch.End();

        _screenFX.EndCapture();

        base.Draw(gameTime);
    }
}
