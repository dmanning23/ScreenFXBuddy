using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;
using System;

namespace ScreenFXBuddy.Effects;

public interface IOverlayLayer
{
    bool IsActive { get; }
    Func<Vector2, Vector2> PositionProvider { get; set; }
    void LoadContent(ContentManager content);
    void Update(GameClock clock);
    void Apply(SpriteBatch spriteBatch);
}
