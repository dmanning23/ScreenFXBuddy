using InputHelper;
using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework.Content;

namespace ScreenFXBuddy.Example
{
    class MainMenuScreen : MenuStackScreen, IMainMenu
    {
        public MainMenuScreen() : base("ScreenFXBuddy Test")
        {
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            var entry2 = new MenuEntry("ChromaticAberration", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ChromaticAberrationScreen());
            });
            AddMenuEntry(entry2);
        }

        public override void ExitScreen()
        {
            //base.ExitScreen();
        }
    }
}