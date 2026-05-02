namespace ScreenFXBuddy;

/// <summary>
/// The way that the effect colors are applied to the screen.
/// </summary>
public enum EffectBlendMode
{
    Multiply,      // Multiplies the color values of the screen and blend color layers
    ColorBurn,     // Darkens the screen to reflect the blend color by increasing contrast
    LinearBurn,    // Darkens the screen by decreasing brightness
    Screen,        // Results in a lighter color similar to projecting multiple slides onto one screen
    ColorDodge,    // Lightens the screen to reflect the blend color by decreasing contrast
    LinearDodge,   // Simply adds the color values together
}