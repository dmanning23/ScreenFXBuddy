using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public interface IOverlayLayer
{
    bool IsActive { get; }
    void LoadContent(ContentManager content);
    void Update(GameClock clock);
    void Apply(SpriteBatch spriteBatch);
}
