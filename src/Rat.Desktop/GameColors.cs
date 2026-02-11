using Raylib_cs;

namespace Rat.Desktop;

/// <summary>
/// Color palette used throughout the game UI.
/// </summary>
internal static class GameColors
{
    // Background colors
    public static readonly Color Background = new(18, 20, 28, 255);
    public static readonly Color GridBackground = new(32, 35, 45, 255);
    public static readonly Color Border = new(25, 28, 35, 255);
    
    // Tile colors
    public static readonly Color Dust = new(85, 75, 65, 255);
    public static readonly Color DustPattern = new(75, 68, 58, 255);
    public static readonly Color DustRevealed = new(65, 58, 50, 255);
    public static readonly Color Dug = new(42, 45, 55, 255);
    public static readonly Color GridLine = new(35, 38, 45, 255);
    
    // HUD colors
    public static readonly Color HudText = new(240, 240, 245, 255);
    public static readonly Color HudDim = new(140, 145, 160, 255);
    public static readonly Color HudPanelBg = new(28, 30, 38, 255);
    public static readonly Color HudPanelHeader = new(35, 38, 48, 255);
    
    // Accent colors
    public static readonly Color Shot = new(255, 70, 70, 255);
    public static readonly Color Success = new(70, 220, 110, 255);
    public static readonly Color Gold = new(255, 215, 70, 255);
    public static readonly Color Cyan = new(70, 200, 255, 255);
    
    // Button colors
    public static readonly Color ButtonPrimary = new(60, 120, 200, 255);
    public static readonly Color ButtonPrimaryHover = new(80, 145, 230, 255);
    public static readonly Color ButtonSuccess = new(50, 180, 100, 255);
    public static readonly Color ButtonSuccessHover = new(70, 210, 120, 255);
    public static readonly Color ButtonNeutral = new(100, 100, 115, 255);
    public static readonly Color ButtonNeutralHover = new(130, 130, 145, 255);
    public static readonly Color ButtonDark = new(90, 90, 100, 255);
    public static readonly Color ButtonDarkHover = new(110, 110, 120, 255);
    
    // Effect colors
    public static readonly Color ShieldAura = new(80, 160, 255, 255);
    public static readonly Color SpeedAura = new(255, 230, 80, 255);
    public static readonly Color DangerGlow = new(255, 40, 40, 255);
    public static readonly Color GemGlow = new(100, 220, 255, 255);
    
    /// <summary>
    /// Creates a color with modified alpha value.
    /// </summary>
    public static Color WithAlpha(Color color, int alpha) =>
        new(color.R, color.G, color.B, (byte)Math.Clamp(alpha, 0, 255));
    
    /// <summary>
    /// Creates a color with modified alpha value (float 0-1).
    /// </summary>
    public static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, (byte)Math.Clamp((int)(alpha * 255), 0, 255));
}
