using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class ZoomBlurScreen : BaseEffectScreen
    {
        public ZoomBlurScreen() : base("ZoomBlur")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var center = new Vector2(1280 / 2f, 720 / 2f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerZoomBlur(center);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerZoomBlur(center, strength: 0.12f, radius: 1.0f, duration: 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerZoomBlur(new Vector2(1280 / 2f, 720 * 0.4f), strength: 0.08f, radius: 0.5f, duration: 0.3f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerZoomBlur(center, strength: -0.12f, radius: 1.0f, duration: 0.5f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.A))
                _screenFX.TriggerZoomBlur(new Vector2(1280 * .25f, 720 / 2f), strength: -0.12f, radius: 1.0f, duration: 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.S))
                _screenFX.TriggerZoomBlur(new Vector2(1280 * .4f, 720 / 2f), strength: -0.12f, radius: 1.0f, duration: 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D))
                _screenFX.TriggerZoomBlur(new Vector2(1280 * .6f, 720 / 2f), strength: -0.12f, radius: 1.0f, duration: 0.5f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
