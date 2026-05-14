using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class GlassShatterScreen : BaseEffectScreen
    {
        public GlassShatterScreen() : base("GlassShatter") { }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerGlassShatter(new Vector2(640, 360));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerGlassShatter(new Vector2(640, 360), strength: 0.03f, numCells: 35, duration: 0.6f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerGlassShatter(new Vector2(640, 360), strength: 0.07f, numCells: 8, duration: 1.2f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
