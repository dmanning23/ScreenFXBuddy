using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ScreenFXBuddy.Example
{
    class VortexScreen : BaseEffectScreen
    {
        public VortexScreen() : base("Vortex") { }

        public override void Update(GameTime gameTime, bool otherWindowHasFocus, bool covered)
        {
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D1))
                _screenFX.TriggerVortex(new Vector2(640, 288));
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D2))
                _screenFX.TriggerVortex(new Vector2(640, 288), strength: 0.5f, radius: 0.35f, speed: -3.0f, duration: 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerVortex(new Vector2(640, 288), strength: 0.20f, radius: 0.15f, speed: 4.0f, duration: 0.35f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerVortex(new Vector2(640, 288), strength: 0.1f, radius: 0.15f, speed: 4.0f, duration: 0.35f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerVortex(new Vector2(640, 288), strength: 0.1f, radius: 0.15f, speed: 2.0f, duration: 0.35f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
                _screenFX.TriggerVortex(new Vector2(640, 288), strength: 0.1f, radius: 0.15f, speed: 1.0f, duration: 0.35f);



            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
