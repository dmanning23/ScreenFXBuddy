namespace ScreenFXBuddy;

/// <summary>
/// The different ways that an effect can be applied to the screen
/// </summary>
public enum FadeMode
{
    FadeIn,    // blend color starts at 0.0 and goes to 1.0
    FadeOut,   // blend color starts at 1.0 and goes to 0.0
    Constant   // blend color is always 1.0
}