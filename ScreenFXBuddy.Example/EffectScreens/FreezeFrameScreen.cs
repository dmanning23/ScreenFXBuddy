using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class FreezeFrameScreen : BaseEffectScreen
    {
        public FreezeFrameScreen() : base("FreezeFrame")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            // D1: icy blue freeze — cryo special
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerFreezeFrame(new Color(100, 160, 255));

            // D2: red freeze — rage / danger moment
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerFreezeFrame(new Color(255, 100, 100), flashIn: 0.05f, hold: 0.60f, fadeOut: 0.40f);

            // D3: white freeze — dramatic finish
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerFreezeFrame(Color.White, flashIn: 0.15f, hold: 0.80f, fadeOut: 0.50f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
