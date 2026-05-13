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

            entry2 = new MenuEntry("ForceRipple", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ForceRippleScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("GravityWave", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new GravityWaveScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("HitFlash", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new HitFlashScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("ScreenShake", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ScreenShakeScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("SpeedLine", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new SpeedLineScreen());
            });
            AddMenuEntry(entry2);
        }

        public override void ExitScreen()
        {
            //base.ExitScreen();
        }
    }
}