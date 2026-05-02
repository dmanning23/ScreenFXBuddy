using GameTimer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public class ChromaticAberrationLayer : IDistortionLayer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Effect _effect;
    private Vector2 _startPosition; // screen-pixel coords, converted to UV in Apply
    private float _distance;        // max UV spread Distance at end of effect

    protected CountdownTimer Timer { get; set; } = new CountdownTimer();

    public bool IsActive => !Timer.Paused && Timer.HasTimeRemaining;

    public ChromaticAberrationLayer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Distorter_ChromaticAberration");
    }

    /// <param name="startPosition">Screen-pixel position the aberration radiates from.</param>
    /// <param name="distance">Max UV-space channel spread at end of effect. Try 0.05–0.2.</param>
    /// <param name="time">Duration in seconds.</param>
    public void Trigger(Vector2 startPosition, float distance = 0.1f, float time = 2f)
    {
        _startPosition = startPosition;
        _distance = distance;
        Timer.Start(time);
    }

    public void Update(GameTime gameTime)
    {
        Timer.Update(gameTime);
    }

    public void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination)
    {
        var viewport = _graphicsDevice.Viewport;
        var originUV = new Vector2(
            _startPosition.X / viewport.Width,
            _startPosition.Y / viewport.Height);

        // Distance grows from 0 → _distance as the timer counts down.
        float currentDistance = _distance * (1f - Timer.Lerp);
        // Strength fades from 1 → 0 as the timer counts down.
        float currentStrength = 1f; //Timer.Lerp;

        _graphicsDevice.SetRenderTarget(destination);
        _effect.Parameters["Origin"].SetValue(originUV);
        _effect.Parameters["Distance"].SetValue(currentDistance);
        _effect.Parameters["Strength"].SetValue(currentStrength);
        _effect.Parameters["SceneTexture"].SetValue(source);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
            _effect);
        spriteBatch.Draw(source, viewport.Bounds, Color.White);
        spriteBatch.End();
    }
}
