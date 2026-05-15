
//using AnimationTool.Screens;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using ResolutionBuddy;
using GameTimer;

namespace ScreenFXBuddy.Example
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    //public class Game1 : ControllerGame

#if __IOS__ || ANDROID || WINDOWS_UAP
	public class ScreenFXTestGame : TouchGame
#else
    public class ScreenFXTestGame : ControllerGame
#endif
    {
        #region Properties

        private ScreenFXComponent _screenFX;

        GameClock timer { get; set; } = new GameClock();

        #endregion //Properties

        #region Methods

        public ScreenFXTestGame()
        {
            IsMouseVisible = true;

            _screenFX = new ScreenFXComponent(this);

            //Graphics.SupportedOrientations = DisplayOrientation.Portrait | DisplayOrientation.PortraitDown;
            //VirtualResolution = new Point(720, 1280);
            //ScreenResolution = new Point(720, 1280);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _screenFX.LoadContent();
            timer.Start();
        }

        protected override void InitStyles()
        {
            StyleSheet.LargeFontResource = @"Fonts\ArialBlack24";
            StyleSheet.MediumFontResource = @"Fonts\ArialBlack14";
            StyleSheet.SmallFontResource = @"Fonts\ArialBlack10";

            StyleSheet.LargeFontSize = 24;
            StyleSheet.MediumFontSize = 14;
            StyleSheet.SmallFontSize = 10;

            StyleSheet.ClickedSoundResource = string.Empty;
            StyleSheet.HighlightedSoundResource = string.Empty;

            base.InitStyles();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            timer.Update(gameTime);
            _screenFX.Update(timer);
        }

        /// <summary>
        /// Get the set of screens needed for the main menu
        /// </summary>
        /// <returns>The gameplay screen stack.</returns>
        public override IScreen[] GetMainMenuScreenStack()
        {
            return new IScreen[] { new BackgroundScreen(), new MainMenuScreen() };
        }

        protected override bool BeginDraw()
        {
            _screenFX.BeginCapture(new Point(Resolution.ScreenArea.Width, Resolution.ScreenArea.Height));

            return base.BeginDraw();
        }

        protected override void EndDraw()
        {
            _screenFX.EndCapture(Resolution.TransformationMatrix(), Resolution.ResetViewport);

            base.EndDraw();
        }

        #endregion //Methods
    }
}
