using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class ForceRippleScreen : BaseEffectScreen
    {
        public ForceRippleScreen() : base("ForceRipple")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var centerPixels = new Vector2(1280 / 2, 720 / 2);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerForceRipple(centerPixels);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerForceRipple(new Vector2(256, 256), 0.1f, 0.4f, 0.08f, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 256), 0.05f, 0.8f, 0.08f, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerForceRipple(new Vector2(256, 720 - 256), 0.05f, 0.4f, 0.16f, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 720 - 256), 0.05f, 0.4f, 0.08f, 4f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
                _screenFX.TriggerForceRipple(new Vector2(256, 256), 0.025f, 0.4f, 0.08f, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D7))
                _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 256), 0.05f, 0.2f, 0.08f, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D8))
                _screenFX.TriggerForceRipple(new Vector2(256, 720 - 256), 0.05f, 0.4f, 0.04f, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D9))
                _screenFX.TriggerForceRipple(new Vector2(1280 - 256, 720 - 256), 0.05f, 0.4f, 0.08f, 1f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}