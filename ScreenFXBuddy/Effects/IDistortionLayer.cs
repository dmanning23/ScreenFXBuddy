using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;
using System;

namespace ScreenFXBuddy.Effects;

public interface IDistortionLayer
{
    bool IsActive { get; }
    Func<Vector2, Vector2> PositionProvider { get; set; }
    void LoadContent(ContentManager content);
    void Update(GameClock clock);
    void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination);
}
