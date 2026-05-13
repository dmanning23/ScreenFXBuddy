using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ScreenFXBuddy.Effects;

namespace ScreenFXBuddy.Example
{
    class SpeedLineScreen : BaseEffectScreen
    {
        public SpeedLineScreen() : base("SpeedLine")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var centerPixels = new Vector2(1280 / 2, 720 / 2);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerSpeedLines(centerPixels, Color.Yellow,
                    SpeedLinesMode.Static, FadeMode.FadeIn, FadeCurve.Linear, 32, 1.0f, 1.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerSpeedLines(new Vector2(300, 200), Color.Cyan,
                    SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Exponential, 48, 0.6f, 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White,
                    SpeedLinesMode.Static, FadeMode.FadeOut, FadeCurve.Linear, 128, 0.8f, 1f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White,
                    SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Linear, 128, 0.8f, 1f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White,
                    SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Logarithmic, 128, 0.8f, 1f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D7))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White,
                    SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Exponential, 128, 0.8f, 1f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D8))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White,
                    SpeedLinesMode.Expand, FadeMode.FadeOut, FadeCurve.Exponential, 128, 0.9f, 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D9))
                _screenFX.TriggerSpeedLines(centerPixels, Color.White,
                    SpeedLinesMode.Static, FadeMode.FadeIn, FadeCurve.Logarithmic, 64, 1f, 0.3f);

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}