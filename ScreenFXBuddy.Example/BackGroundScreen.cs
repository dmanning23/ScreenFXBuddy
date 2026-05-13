using InputHelper;
using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ResolutionBuddy;

namespace ScreenFXBuddy.Example
{
    class BackgroundScreen : Screen
    {
        private Texture2D _background = null!;

        public BackgroundScreen() : base("Background Screen")
        {
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            _background = Content.Load<Texture2D>("Braid_screenshot8");
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin();
            ScreenManager.SpriteBatch.Draw(_background, new Rectangle(0, 0, Resolution.ScreenArea.Width, Resolution.ScreenArea.Height), Color.White);
            ScreenManager.SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}