using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class FrostScreen : BaseEffectScreen
    {
        public FrostScreen() : base("Frost") { }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerFrost(new Vector2(640, 288), new Color(180, 220, 255));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerFrost(new Vector2(640, 288), new Color(100, 160, 255), radius: 0.40f, duration: 2.0f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerFrost(new Vector2(640, 360), Color.White, radius: 0.55f, duration: 2.5f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
