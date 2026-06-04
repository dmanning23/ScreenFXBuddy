using CameraBuddy;
using CollisionBuddy;
using GameTimer;
using HadoukInput;
using MatrixExtensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using PrimitiveBuddy;
using ResolutionBuddy;
using System.Threading.Tasks;
using MenuBuddy;
using ScreenFXBuddy;

namespace CameraExample;

/// <summary>
/// This is the main type for your game
/// </summary>
public class BackgroundCameraScreen : Screen, IMainMenu
{
    #region Members

    Circle _circle1;
    Circle _circle2;

    GameClock _clock;

    InputState _inputState;
    ControllerWrapper _controller;
    InputWrapper _inputWrapper;

    /// <summary>
    /// The camera we are going to use!
    /// </summary>
    Camera _camera;

    Texture2D _texture;
    Primitive primitive;

    /// <summary>
    /// speed to move the circle
    /// </summary>
    const float circleMovementSpeed = 600.0f;

    IScreenFXService _screenFX;

    #endregion //Members

    #region Methods

    public BackgroundCameraScreen()
    {
        _circle1 = new Circle();
        _circle2 = new Circle();

        _clock = new GameClock();

        //Setup the input for this game.
        _inputState = new InputState();
        Mappings.UseKeyboard[0] = true; //Set the first player to use the keyboard. 
        _controller = new ControllerWrapper(0);
        _inputWrapper = new InputWrapper(_controller, _clock.GetCurrentTime);

        //set up the camera
        _camera = new Camera();

        //The WorldBoundary is a rectangle that the Camera will try to stay inside. When the circle moves out of this rectangle it will appear to go offscreen.
        _camera.WorldBoundary = new Rectangle(0, 0, 1280, 720);

        //init the blue circle so it will be on the left of the screen
        _circle1.Initialize(new Vector2(Resolution.TitleSafeArea.Center.X - 300,
                                        Resolution.TitleSafeArea.Center.Y), 60.0f);

        //put the red circle on the right of the screen
        _circle2.Initialize(Resolution.TitleSafeArea.Center, 60.0f);

        //Initialize the camera to start with everything on screen
        AddCircleToCamera(_circle1);
        AddCircleToCamera(_circle2);
        _camera.BeginScene(true); //Pass true to camera.BeginScene to force the camera to instnatly snap to the desired position.

        _clock.Start();
    }

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    public override async Task LoadContent()
    {
        await base.LoadContent();

        primitive = new Primitive(ScreenManager.Game.GraphicsDevice, ScreenManager.SpriteBatch);
        _texture = Content.Load<Texture2D>("Braid_screenshot8");

        _screenFX = ScreenManager.Game.Services.GetService<IScreenFXService>();
    }

    public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
    {
        //update the timer
        _clock.Update(gameTime);

        //update the input
        _inputState.Update();
        _inputWrapper.Update(_inputState, false);

        //check veritcal movement
        if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Up))
        {
            _circle1.Translate(0.0f, -circleMovementSpeed * _clock.TimeDelta);
        }
        else if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Down))
        {
            _circle1.Translate(0.0f, circleMovementSpeed * _clock.TimeDelta);
        }

        //check horizontal movement
        if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Forward))
        {
            _circle1.Translate(circleMovementSpeed * _clock.TimeDelta, 0.0f);
        }
        else if (_inputWrapper.Controller.CheckKeystrokeHeld(EKeystroke.Back))
        {
            _circle1.Translate(-circleMovementSpeed * _clock.TimeDelta, 0.0f);
        }

        /*
        TODO: HERE IS THE BUG:
        The force ripple should be centered over the red circle.
        Instead, the force ripple is always center screen, since that was the location 
        the red circle was initialized at. It is ignoring the camera matrix.
        */
        if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
            _screenFX.TriggerForceRipple(_circle2.Pos);

        //update the camera
        _camera.Update(_clock);

        base.Update(gameTime, otherWindowHasFocus, covered);
    }

    public override void Draw(GameTime gameTime)
    {
        _screenFX.BeginCapture(new Point(Resolution.ScreenArea.Width, Resolution.ScreenArea.Height));

        //1. Add all our points to the camera
        AddCircleToCamera(_circle1);
        AddCircleToCamera(_circle2);

        //2. Update all the matrices of the camera before we start drawing
        _camera.BeginScene(false);

        ScreenManager.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            null, null, null, null,
            _camera.TranslationMatrix * Resolution.TransformationMatrix()); //3. MAGIC SAUCE: Multiply the Camera and Resolution matrixes to transform all the SpriteBatch.Draw calls.

        //Draw the background image so that we can see the camera moving easier
        ScreenManager.SpriteBatch.Draw(_texture, Vector2.Zero, Color.White);

        //draw the players circle in green
        primitive.Circle(_circle1.Pos, _circle1.Radius, Color.Green);

        //draw the stationary circle in red
        primitive.Circle(_circle2.Pos, _circle2.Radius, Color.Red);

        ScreenManager.SpriteBatch.End();

        //Start a new Spriteatch loop to draw our gui! 
        ScreenManager.SpriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            null, null, null, null,
            Resolution.TransformationMatrix()); //Pass in the plain Resolution matrix so the GUI isn't transformed by the Camera.

        primitive.Rectangle(Resolution.TitleSafeArea, Color.Red);

        //Draw the center of the circles as white dots
        DrawCircleCenters();

        ScreenManager.SpriteBatch.End();

        _screenFX.EndCapture(Resolution.TransformationMatrix(), Resolution.ResetViewport);

        base.Draw(gameTime);
    }

    private void AddCircleToCamera(Circle myCircle)
    {
        float pad = (myCircle.Radius * 1.5f); //add a bit of padding so they aren't touching the edge of the screen

        //Add the upperleft and lowerright corners.  That will fit the whole circle in camera
        _camera.AddPoint(myCircle.Pos);
        _camera.AddPoint(new Vector2((myCircle.Pos.X - pad), (myCircle.Pos.Y - pad)));
        _camera.AddPoint(new Vector2((myCircle.Pos.X + pad), (myCircle.Pos.Y + pad)));
    }

    public void DrawCircleCenters()
    {
        //This is a trick to draw bits of GUI over items that have been translated around by the CameraBuddy, for example if you want to draw a name over a character or something.

        var centerPosition1 = MatrixExt.Multiply(_camera.TranslationMatrix, _circle1.Pos); //Use the MatrixExt to get a point that has been transformed by the camera matrix.
        primitive.Circle(centerPosition1, 64, Color.White); //This circle will always be 64px but will be drawn on top of the circle. In theory. This math needs to be cleaned up :/
    }

    #endregion //Methods
}
