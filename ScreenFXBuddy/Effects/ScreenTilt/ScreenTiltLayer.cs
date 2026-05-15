using System;
using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ScreenTiltLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;

    private EffectParameter _pAngle;
    private EffectParameter _pAspectRatio;
    private EffectParameter _pSceneTexture;

    private float TiltDelta { get; set; }

    /// <summary>
    /// this is whether to tilt the camera clockwise or counterclockwise
    /// This flips every time the camera is tilted
    /// It looks cooler if it tilts a different direction each time.
    /// </summary>
    public bool TiltLeft { get; set; } = true;

    /// <summary>
    /// This times the entire camera tilts from start to finish
    /// </summary>
    protected CountdownTimer WholeTimer { get; set; } = new CountdownTimer();

    /// <summary>
    /// This times an individual tilt
    /// </summary>
    public CountdownTimer TiltTimer { get; protected set; } = new CountdownTimer();

    /// <summary>
    /// How hard to tilt the camera.  1.0f for normal amount
    /// </summary>
    public float MaxTiltAngle { get; protected set; }

    /// <summary>
    /// Just tilt the hell out of the camera until I tell you to stop.
    /// </summary>
    private bool EndlessTilt { get; set; }

    /// <summary>
    /// Whether or not the camera is currently shaking.
    /// </summary>
    public bool IsActive => EndlessTilt || (!WholeTimer.Paused && WholeTimer.HasTimeRemaining);

    private const float SnapFraction = 0.1f;

    public ScreenTiltLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_ScreenTilt");
        _pAngle = _effect.Parameters["Angle"];
        _pAspectRatio = _effect.Parameters["AspectRatio"];
        _pSceneTexture = _effect.Parameters["SceneTexture"];
    }

    /// <param name="angle">Peak rotation in degrees. Positive = clockwise. Try 2–5 for subtle, 5–8 for dramatic.</param>
    /// <param name="duration">Total effect duration in seconds.</param>
    public void Trigger(float angle = 3.0f, float duration = 0.4f, float delta = 0.2f)
    {
        TiltDelta = delta;

        //If the screen is currently shaking, don't change this
        if (!IsActive)
        {
            //shake the opposite direction everytime a nre shake shake occurs
            TiltLeft = !TiltLeft;
        }

        //start timing the shake
        if (WholeTimer.HasTimeRemaining)
        {
            MaxTiltAngle = Math.Max(MaxTiltAngle, angle);

            if (WholeTimer.RemainingTime < duration)
            {
                WholeTimerStart(duration);
            }
        }
        else
        {
            MaxTiltAngle = angle;
            WholeTimerStart(duration);
        }

        //start timing the delta
        TiltTimer.Start(delta);
    }

    private void WholeTimerStart(float length)
    {
        if (length > 0)
        {
            EndlessTilt = false;
            WholeTimer.Start(length);
        }
        else
        {
            EndlessTilt = true;
        }
    }

    public void SetTilt(float length, float delta, float amount, bool endless)
    {
        EndlessTilt = endless;

        TiltDelta = delta;

        //shake the opposite direction
        TiltLeft = !TiltLeft;

        //start timing the shake
        MaxTiltAngle = amount;
        WholeTimerStart(length);

        //start timing the delta
        TiltTimer.Start(delta);
    }

    public void StopShake()
    {
        TiltTimer.Stop();
        WholeTimer.Stop();
        EndlessTilt = false;
    }

    public void Update(GameClock clock)
    {
        WholeTimer.Update(clock);
        TiltTimer.Update(clock);

        if (IsActive && !TiltTimer.Paused && !TiltTimer.HasTimeRemaining)
        {
            TiltLeft = !TiltLeft;
            TiltTimer.Start(TiltDelta);
        }
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        if (!IsActive)
        {
            return;
        }

        // Linear fade: full strength at start, zero at end.
        // Endless shake stays at full strength until stopped.
        float fade = EndlessTilt ? 1f : WholeTimer.Lerp;

        float currentAngleDeg = fade * MaxTiltAngle * TiltTimer.Lerp;
        if (TiltLeft)
        {
            currentAngleDeg *= -1;
        }

        var vp = _graphicsDevice.Viewport;
        _graphicsDevice.SetRenderTarget(destination);

        _pAngle.SetValue(MathHelper.ToRadians(currentAngleDeg));
        _pAspectRatio.SetValue((float)vp.Width / vp.Height);
        _pSceneTexture.SetValue(source);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, vp.Bounds, Color.White);
        spriteBatch.End();
    }
}
