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
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.35f, speed: 1.0f, clockwise: false);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D3))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 2.0f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D4))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 2.0f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D5))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 0.7f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D6))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D7))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 0.5f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D8))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 0.5f, fadeCurve: FadeCurve.Logarithmic);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D9))
                _screenFX.TriggerVortex(new Vector2(640, 288), radius: 0.15f, speed: 0.5f, fadeCurve: FadeCurve.Exponential);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.Q))
                _screenFX.TriggerVortex(new Vector2(300, 300), radius: 0.2f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Linear);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.W))
                _screenFX.TriggerVortex(new Vector2(600, 300), radius: 0.2f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Logarithmic);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.E))
                _screenFX.TriggerVortex(new Vector2(900, 300), radius: 0.2f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Exponential);


            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.A))
                _screenFX.TriggerVortex(new Vector2(300, 300), radius: 0.5f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Linear);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.S))
                _screenFX.TriggerVortex(new Vector2(600, 300), radius: 0.5f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Logarithmic);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D))
                _screenFX.TriggerVortex(new Vector2(900, 300), radius: 0.5f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Exponential);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.Z))
                _screenFX.TriggerVortex(new Vector2(300, 300), radius: 0.2f, speed: 0.25f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Linear);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.X))
                _screenFX.TriggerVortex(new Vector2(600, 300), radius: 0.2f, speed: 0.5f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Logarithmic);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.C))
                _screenFX.TriggerVortex(new Vector2(900, 300), radius: 0.2f, speed: 1.0f, spinInTime: 0.5f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Exponential);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.P))
                _screenFX.TriggerVortex(new Vector2(900, 300), radius: 0.3f, speed: 0.2f, spinInTime: 0.1f, spinOutTime: 0.5f, fadeCurve: FadeCurve.Exponential);



            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}
