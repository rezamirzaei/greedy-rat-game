using Rat.Game;
using Raylib_cs;

namespace Rat.Desktop;

/// <summary>
/// Represents the current screen/state of the game.
/// </summary>
internal enum GameState
{
    MainMenu,
    Playing
}

/// <summary>
/// Types of visual effects that can be displayed on the grid.
/// </summary>
internal enum VisualEffectKind
{
    ShotImpact,
    SnakeBite,
    GemSparkle,
    HealthPickup,
    ShieldPickup,
    SpeedBoostPickup,
    RockDug,
    ComboFlash
}

/// <summary>
/// A temporary visual effect displayed at a grid position.
/// </summary>
internal sealed class VisualEffect
{
    public VisualEffectKind Kind { get; }
    public Position Position { get; }
    public float TotalSeconds { get; }
    public float TimeLeftSeconds { get; set; }

    public VisualEffect(VisualEffectKind kind, Position position, float seconds)
    {
        Kind = kind;
        Position = position;
        TotalSeconds = seconds;
        TimeLeftSeconds = seconds;
    }

    /// <summary>
    /// Gets the progress of the effect (1.0 = just started, 0.0 = finished).
    /// </summary>
    public float Progress => TotalSeconds <= 0.0001f ? 0f : Math.Clamp(TimeLeftSeconds / TotalSeconds, 0f, 1f);
    
    /// <summary>
    /// Returns true if the effect has finished.
    /// </summary>
    public bool IsFinished => TimeLeftSeconds <= 0;
}

/// <summary>
/// A particle effect for celebrations and visual feedback.
/// </summary>
internal sealed class ParticleEffect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public Color Color { get; set; }
    public int Size { get; set; }
    public float LifeSeconds { get; set; }

    /// <summary>
    /// Updates particle position and lifetime.
    /// </summary>
    public void Update(float dt)
    {
        X += VelocityX * dt;
        Y += VelocityY * dt;
        VelocityY += GameConstants.ParticleGravity * dt;
        LifeSeconds -= dt;
    }

    /// <summary>
    /// Returns true if the particle should be removed.
    /// </summary>
    public bool ShouldRemove(int screenHeight) => LifeSeconds <= 0 || Y > screenHeight + 50;
    
    /// <summary>
    /// Gets the current alpha value based on remaining life.
    /// </summary>
    public byte Alpha => (byte)(255 * Math.Min(1, LifeSeconds / 0.5f));
}

/// <summary>
/// Floating text that appears and fades out (e.g., "+100", "SHIELD!").
/// </summary>
internal sealed class FloatingText
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Text { get; set; } = "";
    public Color Color { get; set; }
    public float LifeSeconds { get; set; }
    public float TotalLife { get; set; }

    /// <summary>
    /// Updates position and lifetime.
    /// </summary>
    public void Update(float dt)
    {
        Y -= GameConstants.FloatingTextSpeed * dt;
        LifeSeconds -= dt;
    }

    /// <summary>
    /// Returns true if the text should be removed.
    /// </summary>
    public bool IsFinished => LifeSeconds <= 0;

    /// <summary>
    /// Gets the current alpha value with ease-out effect.
    /// </summary>
    public byte Alpha
    {
        get
        {
            var progress = 1f - (LifeSeconds / TotalLife);
            return (byte)(255 * (1f - progress * progress));
        }
    }
}

/// <summary>
/// Tracks an item that was just consumed, keeping it visible temporarily.
/// </summary>
internal sealed class ConsumedItem
{
    public Position Position { get; }
    public CellContent Content { get; }
    public float TotalSeconds { get; }
    public float TimeLeftSeconds { get; set; }

    public ConsumedItem(Position position, CellContent content, float seconds)
    {
        Position = position;
        Content = content;
        TotalSeconds = seconds;
        TimeLeftSeconds = seconds;
    }

    /// <summary>
    /// Gets the progress of visibility (1.0 = just consumed, 0.0 = fully faded).
    /// </summary>
    public float Progress => TotalSeconds <= 0.0001f ? 0f : Math.Clamp(TimeLeftSeconds / TotalSeconds, 0f, 1f);
    
    /// <summary>
    /// Returns true if the item should be removed.
    /// </summary>
    public bool IsFinished => TimeLeftSeconds <= 0;
    
    /// <summary>
    /// Gets the current alpha value.
    /// </summary>
    public byte Alpha => (byte)(255 * Progress);
    
    /// <summary>
    /// Gets the current scale (shrinks as it fades).
    /// </summary>
    public float Scale => 0.9f + 0.2f * Progress;
}
