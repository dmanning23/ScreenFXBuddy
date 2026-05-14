using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class SmokeScreen : BaseEffectScreen
    {
        public SmokeScreen() : base("Smoke") { }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerSmoke(new Vector2(640, 540), new Color(120, 120, 120));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerSmoke(new Vector2(640, 540), new Color(40, 35, 30), radius: 0.25f, duration: 3.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerSmoke(new Vector2(640, 540), Color.White, radius: 0.1f, duration: 1.0f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerSmoke(new Vector2(640, 540), Color.WhiteSmoke, radius: 10f, duration: 1.0f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerSmoke(new Vector2(640, 540), Color.WhiteSmoke, radius: 10f, duration: 5f);


            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
