using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class ElectricScreen : BaseEffectScreen
    {
        public ElectricScreen() : base("Electric") { }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerElectric(new Vector2(640, 288), new Color(100, 200, 255));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerElectric(new Vector2(640, 288), new Color(255, 200, 50), radius: 0.35f, duration: 0.80f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerElectric(new Vector2(640, 288), new Color(200, 100, 255), radius: 0.15f, duration: 0.40f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
