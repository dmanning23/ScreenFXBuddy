using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ScreenFXBuddy.Effects;

namespace ScreenFXBuddy;

public interface IScreenFXService
{
    List<IDistortionLayer> DistortionLayers { get; }
    List<IOverlayLayer> OverlayLayers { get; }

    ForceRippleLayer ForceRipple { get; }
    GravityWaveLayer GravityWave { get; }
    ScreenShakeLayer ScreenShake { get; }
    ChromaticAberrationLayer ChromaticAberration { get; }
    HeatHazeLayer HeatHaze { get; }
    HitFlashLayer HitFlash { get; }
    AnimeSuperLayer AnimeSuper { get; }
    SpeedLinesLayer SpeedLines { get; }
    LetterboxLayer Letterbox { get; }
    FreezeFrameLayer FreezeFrame { get; }
    ZoomBlurLayer ZoomBlur { get; }
    ScreenTiltLayer ScreenTilt { get; }
    ElectricLayer Electric { get; }
    FrostLayer Frost { get; }
    VortexLayer Vortex { get; }

    void TriggerForceRipple(
        Vector2 position,
        float strength = 0.05f,
        float speed = 0.4f,
        float size = 0.08f,
        float time = 2f);

    void TriggerGravityWave(
        Vector2 position,
        float strength = 0.04f,
        float startHeight = 0.05f,
        float endHeight = 0.25f,
        float speed = 0.5f,
        float duration = 1.5f);

    void TriggerScreenShake(
        float length = 1f,
        float delta = 0.1f,
        float amount = 0.1f);

    void TriggerChromaticAberration(
        Vector2 startPosition,
        float distance = 0.1f,
        float time = 2f,
        FadeCurve curve = FadeCurve.Linear);

    public void TriggerHeatHaze(
        Vector2 position,
        float strength = 0.02f,
        float radius   = 0.15f,
        float height   = 0.40f,
        float duration = 3.0f);

    public void TriggerHitFlash(
        Color blendColor,
        FadeMode mode = FadeMode.FadeOut,
        FadeCurve curve = FadeCurve.Linear,
        EffectBlendMode blendMode = EffectBlendMode.LinearDodge,
        float time = 1f);

    void TriggerAnimeSuper(Color color, float flashIn = 0.05f, float hold = 0.30f, float fadeOut = 0.40f);

    void TriggerLetterbox(float barHeight = 0.10f, float slideIn = 0.15f, float hold = 1.00f, float slideOut = 0.15f);

    void TriggerFreezeFrame(Color tintColor, float flashIn = 0.10f, float hold = 0.40f, float fadeOut = 0.30f);

    void TriggerZoomBlur(Vector2 position, float strength = 0.05f, float radius = 1.0f, float duration = 0.4f);
    void TriggerChromaticSplit(Vector2 position, float maxDistance = 0.05f, float duration = 0.3f);
    void TriggerScreenTilt(float angle = 3.0f, float duration = 0.4f);
    void TriggerElectric(Vector2 position, Color color, float radius = 0.20f, float duration = 0.60f);
    void TriggerFrost(Vector2 position, Color tintColor, float radius = 0.25f, float duration = 1.50f);
    void TriggerVortex(Vector2 position, float strength = 0.30f, float radius = 0.25f, float speed = 2.00f, float duration = 0.60f);

    public void TriggerSpeedLines(
        Vector2 position,
        Color color,
        SpeedLinesMode linesMode = SpeedLinesMode.Expand,
        FadeMode fadeMode = FadeMode.FadeOut,
        FadeCurve fadeCurve = FadeCurve.Logarithmic,
        int lineCount = 24,
        float maxRadius = 1.0f,
        float duration = 1f);
}
