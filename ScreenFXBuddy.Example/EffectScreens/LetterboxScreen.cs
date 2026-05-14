using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class LetterboxScreen : BaseEffectScreen
    {
        public LetterboxScreen() : base("Letterbox")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            // D1: default letterbox — cinematic super intro
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerLetterbox();

            // D2: slow dramatic letterbox
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerLetterbox(barHeight: 0.12f, slideIn: 0.30f, hold: 2.00f, slideOut: 0.30f);

            // D3: quick snap letterbox
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerLetterbox(barHeight: 0.08f, slideIn: 0.05f, hold: 0.50f, slideOut: 0.20f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
