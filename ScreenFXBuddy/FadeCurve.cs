namespace ScreenFXBuddy;

/// <summary>
/// The curve that an effect will follow when fading in/out.
/// </summary>
public enum FadeCurve
{
    Linear,       // constant rate of change
    Logarithmic,  // fast initial change that decelerates (log curve)
    Exponential   // slow initial change that accelerates (quadratic ease-in)
}