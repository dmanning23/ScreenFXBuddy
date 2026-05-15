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
                _screenFX.TriggerScreenTilt(angle: 6.0f, duration: 0.4f, 0.2f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerScreenTilt(angle: 6.0f, duration: 0.4f, 0.4f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerScreenTilt(angle: 12.0f, duration: 0.4f, 0.2f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerScreenTilt(angle: 12.0f, duration: 0.4f, 0.4f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
                _screenFX.TriggerScreenTilt(angle: 6.0f, duration: 1f, 0.1f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
