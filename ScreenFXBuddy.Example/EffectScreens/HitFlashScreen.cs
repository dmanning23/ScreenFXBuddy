using System.Threading.Tasks;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class HitFlashScreen : BaseEffectScreen
    {
        public HitFlashScreen() : base("HitFlash")
        {
        }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            var centerPixels = new Vector2(1280 / 2, 720 / 2);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.LinearDodge, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Logarithmic, EffectBlendMode.LinearDodge, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Exponential, EffectBlendMode.LinearDodge, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.ColorBurn, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.ColorDodge, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.LinearBurn, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D7))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.Multiply, 2f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D8))
                _screenFX.TriggerHitFlash(Color.White, FadeMode.FadeOut, FadeCurve.Linear, EffectBlendMode.Screen, 2f);
            // if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D9))
            //     _screenFX.TriggerChromaticAberration(centerPixels, 1f, 1f, FadeCurve.Exponential);   // right-of-center

            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}