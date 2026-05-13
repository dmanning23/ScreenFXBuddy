using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework;
using ResolutionBuddy;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    abstract class BaseEffectScreen : WidgetScreen
    {
        protected IScreenFXService _screenFX;

        public BaseEffectScreen(string name) : base(name)
        {
            CoverOtherScreens = true;
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            _screenFX = ScreenManager.Game.Services.GetService<IScreenFXService>();

            //Add a label in the upper center
            var screenNameLabel = new Label(ScreenName, this.Content)
            {
                Clickable = false,
                Highlightable = false,
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Top,
                Position = new Point(Resolution.TitleSafeArea.Center.X, 0),
            };
            AddItem(screenNameLabel);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.Escape))
            {
                ExitScreen();
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

    }
}