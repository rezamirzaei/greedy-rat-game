namespace Rat.Desktop;

/// <summary>
/// Layout and timing constants used throughout the game.
/// </summary>
internal static class GameConstants
{
    // Layout
    public const int TileSize = 40;
    public const int Margin = 20;
    public const int SidePanelWidth = 320;
    public const int ButtonWidth = 220;
    public const int ButtonHeight = 50;
    
    // Window
    public const int MinWindowWidth = 950;
    public const int MinWindowHeight = 650;
    public const int InitialWindowWidth = 1000;
    public const int InitialWindowHeight = 700;
    public const int TargetFps = 60;
    
    // Timing - Overlays
    public const float OverlayFadeSpeed = 3.5f;
    public const float ChapterCompleteDelay = 1.8f;
    public const float GameOverDelay = 1.2f;
    public const float LevelTransitionSpeed = 3f;
    
    // Timing - Effects
    public const float ConsumedItemDuration = 1.5f;
    public const float GemConsumedDuration = 2.0f;
    public const float FloatingTextDuration = 1.2f;
    public const float FloatingTextSpeed = 40f;
    public const float CelebrationParticleDuration = 4f;
    
    // Timing - Combat
    public const float RatHitFlashDuration = 0.28f;
    public const float SnakeBiteFlashDuration = 0.35f;
    public const float ShakeDuration = 0.30f;
    
    // Animation speeds
    public const float RatAnimationSpeed = 6f;
    public const float SnakeAnimationSpeed = 4f;
    public const float GemAnimationSpeed = 4f;
    public const float MenuAnimationSpeed = 2f;
    
    // Physics
    public const float ParticleGravity = 80f;
    public const float ShakeBaseStrength = 6f;
}
