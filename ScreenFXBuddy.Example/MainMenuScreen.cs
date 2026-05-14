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

            entry2 = new MenuEntry("AnimeSuper", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new AnimeSuperScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("Letterbox", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new LetterboxScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("FreezeFrame", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new FreezeFrameScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("ZoomBlur", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ZoomBlurScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("ChromaticSplit", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ChromaticSplitScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("ScreenTilt", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ScreenTiltScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("Electric", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new ElectricScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("Frost", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new FrostScreen());
            });
            AddMenuEntry(entry2);

            entry2 = new MenuEntry("Vortex", Content);
            entry2.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(new VortexScreen());
            });
            AddMenuEntry(entry2);
        }

        public override void ExitScreen()
        {
            //base.ExitScreen();
        }
    }
}