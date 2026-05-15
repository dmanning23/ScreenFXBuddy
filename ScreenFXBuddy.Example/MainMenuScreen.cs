using InputHelper;
using System;
using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework.Content;
using ResolutionBuddy;
using Microsoft.Xna.Framework;

namespace ScreenFXBuddy.Example
{
    class MainMenuScreen : WidgetScreen, IMainMenu
    {
        public MainMenuScreen() : base("ScreenFXBuddy Test")
        {
            CoveredByOtherScreens = true;
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            var stack1 = new StackLayout(StackAlignment.Top)
            {
                Horizontal = HorizontalAlignment.Left,
                Vertical = VerticalAlignment.Top,
                Position = new Point(Resolution.TitleSafeArea.Left, Resolution.TitleSafeArea.Top),
                Highlightable = true,
                Clickable = true,
                HasOutline = true,
            };

            AddScreenButton(stack1, "ChromaticAberration", () => new ChromaticAberrationScreen());
            AddScreenButton(stack1, "ForceRipple", () => new ForceRippleScreen());
            AddScreenButton(stack1, "GravityWave", () => new GravityWaveScreen());
            AddScreenButton(stack1, "HitFlash", () => new HitFlashScreen());
            AddScreenButton(stack1, "ScreenShake", () => new ScreenShakeScreen());
            AddScreenButton(stack1, "SpeedLine", () => new SpeedLineScreen());
            AddScreenButton(stack1, "AnimeSuper", () => new AnimeSuperScreen());
            AddScreenButton(stack1, "Letterbox", () => new LetterboxScreen());
            AddScreenButton(stack1, "FreezeFrame", () => new FreezeFrameScreen());
            AddScreenButton(stack1, "ZoomBlur", () => new ZoomBlurScreen());
            AddScreenButton(stack1, "ChromaticSplit", () => new ChromaticSplitScreen());
            AddScreenButton(stack1, "ScreenTilt", () => new ScreenTiltScreen());
            AddScreenButton(stack1, "Electric", () => new ElectricScreen());
            AddScreenButton(stack1, "Frost", () => new FrostScreen());
            AddScreenButton(stack1, "Vortex", () => new VortexScreen());
            AddScreenButton(stack1, "HeatHaze", () => new HeatHazeScreen());
            AddScreenButton(stack1, "Smoke", () => new SmokeScreen());
            AddScreenButton(stack1, "GlassShatter", () => new GlassShatterScreen());

            AddItem(stack1);
        }

        private void AddScreenButton(StackLayout stack, string text, Func<IScreen> screenFactory)
        {
            var button = new RelativeLayoutButton()
            {
                Name = text,
                TransitionObject = new WipeTransitionObject(TransitionWipeType.PopLeft),
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Center,
                Clickable = true,
                HasOutline = true,
                Highlightable = true,
                IsTappable = true
            };
            var label = new Label(text, Content)
            {
                Horizontal = HorizontalAlignment.Center,
                Vertical = VerticalAlignment.Center,
                Highlightable = true,
            };
            button.Size = label.Rect.Size.ToVector2();
            button.AddItem(label);

            button.OnClick += ((object obj, ClickEventArgs e) =>
            {
                ScreenManager.AddScreen(screenFactory());
            });
            stack.AddItem(button);
        }

        public override void ExitScreen()
        {
            //base.ExitScreen();
        }
    }
}