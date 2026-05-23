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


            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.Q))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.015f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.W))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.03f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.E))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.06f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.R))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.12f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.T))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.3f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.Y))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.6f, startHeight: 0.05f, endHeight: 0.12f, speed: 0.4f, duration: 0.8f);


            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.A))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.09f, startHeight: 0.2f, endHeight: 0.25f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.S))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.9f, startHeight: 0.1f, endHeight: 0.05f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.D))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.9f, startHeight: 0.1f, endHeight: 0.05f, speed: 0.2f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.F))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.9f, startHeight: 0.1f, endHeight: 0.05f, speed: 0.8f, duration: 0.8f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.Z))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.09f, startHeight: 0.2f, endHeight: 0.05f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.X))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.1f, startHeight: 0.2f, endHeight: 0.0f, speed: 0.4f, duration: 0.8f);

            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.C))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.1f, startHeight: 0f, endHeight: 0.3f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.V))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.1f, startHeight: 0.05f, endHeight: 0.3f, speed: 0.4f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.B))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.15f, startHeight: 0.05f, endHeight: 0.5f, speed: 0.5f, duration: 0.8f);
            if (ScreenManager.Input.InputState.IsNewKeyPress(Keys.N))
                _screenFX.TriggerGravityWave(new Vector2(1280 / 2f, 720 * 0.75f),
                    strength: 0.15f, startHeight: 0.05f, endHeight: 0.5f, speed: 0.3f, duration: 0.8f);
            base.Update(gameTime, otherWindowHasFocus, covered);
        }
    }
}