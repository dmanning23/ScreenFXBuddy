using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private ScreenFXComponent _screenFX;
    private KeyboardState _prevKeys;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _screenFX = new ScreenFXComponent(this);
        Components.Add(_screenFX);
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (keys.IsKeyDown(Keys.Escape)) Exit();

        var center = new Vector2(0.5f, 0.5f);

        if (keys.IsKeyDown(Keys.D1) && !_prevKeys.IsKeyDown(Keys.D1))
            _screenFX.TriggerForceRipple(center);

        if (keys.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2))
            _screenFX.TriggerGravityWave(center);

        if (keys.IsKeyDown(Keys.D3) && !_prevKeys.IsKeyDown(Keys.D3))
            _screenFX.TriggerScreenShake();

        if (keys.IsKeyDown(Keys.D4) && !_prevKeys.IsKeyDown(Keys.D4))
            _screenFX.TriggerChromaticAberration(1f, 2f);

        if (keys.IsKeyDown(Keys.D5) && !_prevKeys.IsKeyDown(Keys.D5))
            _screenFX.TriggerHeatHaze(1f, 2f);

        if (keys.IsKeyDown(Keys.D6) && !_prevKeys.IsKeyDown(Keys.D6))
            _screenFX.TriggerHitFlash(Color.White, 0.5f);

        if (keys.IsKeyDown(Keys.D7) && !_prevKeys.IsKeyDown(Keys.D7))
            _screenFX.TriggerAnimeSuper(Color.White, 1f);

        _prevKeys = keys;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _screenFX.BeginCapture();

        GraphicsDevice.Clear(Color.CornflowerBlue);
        // TODO: draw game scene here

        _screenFX.EndCapture();

        base.Draw(gameTime);
    }
}
