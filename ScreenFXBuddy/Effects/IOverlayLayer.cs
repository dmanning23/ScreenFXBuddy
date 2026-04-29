using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ScreenFXBuddy.Effects;

public interface IOverlayLayer
{
    bool IsActive { get; }
    void LoadContent(ContentManager content);
    void Update(GameTime gameTime);
    void Apply(SpriteBatch spriteBatch);
}
