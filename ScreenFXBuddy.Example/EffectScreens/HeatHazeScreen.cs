using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class HeatHazeScreen : BaseEffectScreen
    {
        public HeatHazeScreen() : base("HeatHaze") { }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerHeatHaze(new Vector2(640, 540));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerHeatHaze(new Vector2(640, 540), strength: 0.05f, radius: 0.3f, height: 0.7f, duration: 4.0f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerHeatHaze(new Vector2(640, 540), strength: 0.04f, radius: 0.06f, height: 0.25f, duration: 2.0f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
