using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class ScreenShakeScreen : BaseEffectScreen
    {
        public ScreenShakeScreen() : base("ScreenShake")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var centerPixels = new Vector2(1280 / 2, 720 / 2);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerScreenShake();
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerScreenShake(2f, 0.5f, 0.3f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerScreenShake(0.5f, 0.1f, 0.05f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerScreenShake(1f, 1f, 0.6f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerScreenShake(0.1f, 0.05f, 0.1f);
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
            //     _screenFX.TriggerChromaticAberration(new Vector2(960, 360), 1f, 0.6f, FadeCurve.Logarithmic);   // right-of-center
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D7))
            //     _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f);
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D8))
            //     _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f, FadeCurve.Logarithmic);   // right-of-center
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D9))
            //     _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f, FadeCurve.Exponential);   // right-of-center

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}