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
    private EffectParameter _pOffset;

    // UV-space scale applied to ShakeAmount to produce the final offset.
    // ShakeAmount=1.0 results in a 4% screen-width shake at peak.
    private const float UvScale = 0.04f;

    private float ShakeDelta { get; set; }

    // Tracked so Apply can compute a linear fade-out over the shake duration.
    private float _wholeDuration;

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
        _effect = content.Load<Effect>("Distorter_ScreenShake");
        _pOffset = _effect.Parameters["Offset"];
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
            _wholeDuration = length;
            WholeTimer.Start(length);
        }
        else
        {
            EndlessShake = true;
            _wholeDuration = 0f;
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
        // Linear fade: full strength at start, zero at end.
        // Endless shake stays at full strength until stopped.
        float fade = (EndlessShake || _wholeDuration <= 0f)
            ? 1f
            : WholeTimer.RemainingTime / _wholeDuration;

        var totalShake = UvScale * fade * ShakeAmount;

        //figure out the proper rotation for the camera shake
        var shakeX = (totalShake *
            (float)Math.Sin(
            ((ShakeTimer.CurrentTime * (2.0f * Math.PI)) /
            ShakeTimer.CountdownLength)));

        var shakeY = (totalShake *
            (float)Math.Cos(
            ((ShakeTimer.CurrentTime * (2.0f * Math.PI)) /
            ShakeTimer.CountdownLength)));

        shakeX = shakeX * (ShakeLeft ? -1.0f : 1.0f);

        var offset = new Vector2(shakeX, shakeY);

        _graphicsDevice.SetRenderTarget(destination);
        _pOffset.SetValue(offset);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, _graphicsDevice.Viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
