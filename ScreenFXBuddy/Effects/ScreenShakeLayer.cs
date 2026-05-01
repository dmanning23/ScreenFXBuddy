using GameTimer;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

/// <summary>
/// This is a screen effect that shakes the screen.
/// </summary>
public class ScreenShakeLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private float ShakeDelta { get; set; }

    /// <summary>
    /// this is whether to shake the camera clockwise or counterclockwise
    /// This flips every time the camera is shaken
    /// It looks cooler if it shakes a different direction each time.
    /// </summary>
    public bool ShakeLeft { get; set; } = true;

    /// <summary>
    /// This times the entire camera shake from start to finish
    /// </summary>
    protected CountdownTimer WholeTimer { get; set; } = new CountdownTimer();

    /// <summary>
    /// This times an individual shake
    /// </summary>
    public CountdownTimer ShakeTimer { get; protected set; } = new CountdownTimer();

    /// <summary>
    /// How hard to shake the camera.  1.0f for normal amount
    /// </summary>
    public float ShakeAmount { get; protected set; } = 1f;

    /// <summary>
    /// Just shake the hell out of the camera until I tell you to stop.
    /// </summary>
    private bool EndlessShake { get; set; }

    /// <summary>
    /// Whether or not the camera is currently shaking.
    /// </summary>
    public bool IsActive => EndlessShake || (!WholeTimer.Paused && WholeTimer.HasTimeRemaining);

    public ScreenShakeLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Debug_Color");
    }

    /// <summary>
    /// Shake the screen!
    /// The defaults here are a polite, but stern, little screen shake.
    /// </summary>
    /// <param name="length">How long the entire screen shake effect should last</param>
    /// <param name="delta">How long each individual shake in the effect lasts</param>
    /// <param name="amount">How hard to shake the screen</param>
    public void Trigger(float length = 1f, float delta = 0.1f, float amount = 0.1f)
    {
        ShakeDelta = delta;

        //shake the opposite direction
        ShakeLeft = !ShakeLeft;

        //start timing the shake
        if (WholeTimer.HasTimeRemaining)
        {
            ShakeAmount = Math.Max(ShakeAmount, amount);

            if (WholeTimer.RemainingTime < length)
            {
                WholeTimerStart(length);
            }
        }
        else
        {
            ShakeAmount = amount;
            WholeTimerStart(length);
        }

        //start timing the delta
        ShakeTimer.Start(delta);
    }

    public void SetShake(float length, float delta, float amount)
    {
        ShakeDelta = delta;

        //shake the opposite direction
        ShakeLeft = !ShakeLeft;

        //start timing the shake
        ShakeAmount = amount;
        WholeTimerStart(length);

        //start timing the delta
        ShakeTimer.Start(delta);
    }

    private void WholeTimerStart(float length)
    {
        if (length > 0)
        {
            EndlessShake = false;
            WholeTimer.Start(length);
        }
        else
        {
            EndlessShake = true;
        }
    }

    public void StopShake()
    {
        ShakeTimer.Stop();
        WholeTimer.Stop();
        EndlessShake = false;
    }

    public void Update(GameTime gameTime)
    {
        WholeTimer.Update(gameTime);
        ShakeTimer.Update(gameTime);

        if (IsActive && !ShakeTimer.Paused && !ShakeTimer.HasTimeRemaining)
        {
            ShakeLeft = !ShakeLeft;
            ShakeTimer.Start(ShakeDelta);
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        //TODO: calculate the entire amount of shake to add to the scene

        _graphicsDevice.SetRenderTarget(destination);

        _effect.Parameters["DebugColor"].SetValue(Color.Purple.ToVector4());
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
