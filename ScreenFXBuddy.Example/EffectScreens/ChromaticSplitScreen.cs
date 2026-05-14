using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class ChromaticSplitScreen : BaseEffectScreen
    {
        public ChromaticSplitScreen() : base("ChromaticSplit")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var center = new Vector2(1280 / 2f, 720 / 2f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerChromaticSplit(center);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerChromaticSplit(center, maxDistance: 0.09f, duration: 0.5f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerChromaticSplit(center, maxDistance: 0.025f, duration: 0.2f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
