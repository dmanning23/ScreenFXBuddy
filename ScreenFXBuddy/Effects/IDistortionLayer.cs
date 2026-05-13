using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameTimer;

namespace ScreenFXBuddy.Effects;

public interface IDistortionLayer
{
    bool IsActive { get; }
    void LoadContent(ContentManager content);
    void Update(GameClock clock);
    void Apply(SpriteBatch spriteBatch, RenderTarget2D source, RenderTarget2D destination);
}
