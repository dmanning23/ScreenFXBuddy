using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class AnimeSuperScreen : BaseEffectScreen
    {
        public AnimeSuperScreen() : base("AnimeSuper")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            // D1: white flash — standard super
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerAnimeSuper(Color.White);

            // D2: red flash — rage super
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerAnimeSuper(new Color(255, 80, 80), flashIn: 0.08f, hold: 0.50f, fadeOut: 0.60f);

            // D3: gold flash — ultra / power super
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerAnimeSuper(new Color(255, 220, 80), flashIn: 0.12f, hold: 0.80f, fadeOut: 1.00f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
