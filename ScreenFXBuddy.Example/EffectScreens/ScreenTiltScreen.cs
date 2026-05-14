using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class ScreenTiltScreen : BaseEffectScreen
    {
        public ScreenTiltScreen() : base("ScreenTilt")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerScreenTilt();

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerScreenTilt(angle: 6.0f, duration: 0.5f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerScreenTilt(angle: -3.0f, duration: 0.4f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
