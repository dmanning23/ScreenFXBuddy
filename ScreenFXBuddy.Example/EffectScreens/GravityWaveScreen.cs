using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class GravityWaveScreen : BaseEffectScreen
    {
        public GravityWaveScreen() : base("GravityWave")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var centerPixels = new Vector2(1280 / 2, 720 / 2);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.06f, startHeight: 0.02f, endHeight: 0.4f, speed: 0.3f, duration: 2.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.03f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.9f, duration: 0.8f);
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
            //     _screenFX.TriggerChromaticAberration(centerPixels, 4f, 2f);
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
            //     _screenFX.TriggerChromaticAberration(new Vector2(320, 360), 1f, 0.4f, FadeCurve.Exponential);   // left-of-center
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