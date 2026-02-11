using Rat.Game;
using Raylib_cs;

namespace Rat.Desktop;

/// <summary>
/// Manages visual effects, particles, floating texts, and consumed items.
/// </summary>
internal sealed class EffectsManager
{
    private readonly List<VisualEffect> _effects = new();
    private readonly List<ParticleEffect> _particles = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly List<ConsumedItem> _consumedItems = new();
    
    private float _ratHitFlashSecondsLeft;
    private double _shakeSecondsLeft;
    private float _shakeStrength = GameConstants.ShakeBaseStrength;
    private System.Numerics.Vector2 _shakeOffset;

    public IReadOnlyList<VisualEffect> Effects => _effects;
    public IReadOnlyList<ParticleEffect> Particles => _particles;
    public IReadOnlyList<FloatingText> FloatingTexts => _floatingTexts;
    public IReadOnlyList<ConsumedItem> ConsumedItems => _consumedItems;
    
    public float RatHitFlash => _ratHitFlashSecondsLeft;
    public System.Numerics.Vector2 ShakeOffset => _shakeOffset;

    /// <summary>
    /// Clears all effects (call when starting new game/chapter).
    /// </summary>
    public void Clear()
    {
        _effects.Clear();
        _particles.Clear();
        _floatingTexts.Clear();
        _consumedItems.Clear();
        _ratHitFlashSecondsLeft = 0;
        _shakeSecondsLeft = 0;
        _shakeOffset = System.Numerics.Vector2.Zero;
    }

    /// <summary>
    /// Updates all effects, removing finished ones.
    /// </summary>
    public void Update(float dt, double elapsedSeconds)
    {
        UpdateRatFlash(dt);
        UpdateEffects(dt);
        UpdateParticles(dt);
        UpdateFloatingTexts(dt);
        UpdateConsumedItems(dt);
        UpdateShake(dt, elapsedSeconds);
    }

    private void UpdateRatFlash(float dt)
    {
        if (_ratHitFlashSecondsLeft > 0)
            _ratHitFlashSecondsLeft = Math.Max(0, _ratHitFlashSecondsLeft - dt);
    }

    private void UpdateEffects(float dt)
    {
        for (var i = _effects.Count - 1; i >= 0; i--)
        {
            _effects[i].TimeLeftSeconds -= dt;
            if (_effects[i].IsFinished)
                _effects.RemoveAt(i);
        }
    }

    private void UpdateParticles(float dt)
    {
        var screenHeight = Raylib.GetScreenHeight();
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(dt);
            if (_particles[i].ShouldRemove(screenHeight))
                _particles.RemoveAt(i);
        }
    }

    private void UpdateFloatingTexts(float dt)
    {
        for (var i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Update(dt);
            if (_floatingTexts[i].IsFinished)
                _floatingTexts.RemoveAt(i);
        }
    }

    private void UpdateConsumedItems(float dt)
    {
        for (var i = _consumedItems.Count - 1; i >= 0; i--)
        {
            _consumedItems[i].TimeLeftSeconds -= dt;
            if (_consumedItems[i].IsFinished)
                _consumedItems.RemoveAt(i);
        }
    }

    private void UpdateShake(float dt, double elapsedSeconds)
    {
        if (_shakeSecondsLeft <= 0)
        {
            _shakeOffset = System.Numerics.Vector2.Zero;
            _shakeStrength = GameConstants.ShakeBaseStrength;
            return;
        }

        _shakeSecondsLeft = Math.Max(0, _shakeSecondsLeft - dt);
        var fade = (float)Math.Clamp(_shakeSecondsLeft / GameConstants.ShakeDuration, 0.0, 1.0);
        _shakeOffset = new System.Numerics.Vector2(
            MathF.Sin((float)elapsedSeconds * 55f) * _shakeStrength * fade,
            MathF.Cos((float)elapsedSeconds * 60f) * _shakeStrength * fade
        );
    }

    /// <summary>
    /// Starts a screen shake effect.
    /// </summary>
    public void StartShake(double seconds, float strength)
    {
        _shakeSecondsLeft = Math.Max(_shakeSecondsLeft, seconds);
        _shakeStrength = Math.Max(_shakeStrength, strength);
    }

    /// <summary>
    /// Adds a visual effect at the specified position.
    /// </summary>
    public void AddEffect(VisualEffectKind kind, Position position, float seconds)
    {
        _effects.Add(new VisualEffect(kind, position, seconds));
    }

    /// <summary>
    /// Spawns floating text at the specified grid position.
    /// </summary>
    public void SpawnFloatingText(Position pos, string text, Color color, System.Numerics.Vector2 shakeOffset)
    {
        _floatingTexts.Add(new FloatingText
        {
            X = GameConstants.Margin + (int)shakeOffset.X + pos.X * GameConstants.TileSize + GameConstants.TileSize / 2,
            Y = GameConstants.Margin + (int)shakeOffset.Y + pos.Y * GameConstants.TileSize,
            Text = text,
            Color = color,
            LifeSeconds = GameConstants.FloatingTextDuration,
            TotalLife = GameConstants.FloatingTextDuration
        });
    }

    /// <summary>
    /// Adds a consumed item to display temporarily.
    /// </summary>
    public void AddConsumedItem(Position position, CellContent content, float duration)
    {
        _consumedItems.Add(new ConsumedItem(position, content, duration));
    }

    /// <summary>
    /// Triggers rat hit flash effect.
    /// </summary>
    public void TriggerRatHitFlash(float duration)
    {
        _ratHitFlashSecondsLeft = duration;
    }

    /// <summary>
    /// Spawns celebration particles (for chapter complete).
    /// </summary>
    public void SpawnCelebrationParticles(int count = 2)
    {
        var random = new Random();
        var screenWidth = Raylib.GetScreenWidth();
        
        for (var i = 0; i < count; i++)
        {
            var color = (i % 3) switch
            {
                0 => GameColors.Gold,
                1 => GameColors.Success,
                _ => GameColors.Cyan
            };
            
            _particles.Add(new ParticleEffect
            {
                X = random.Next(0, screenWidth),
                Y = -10,
                VelocityX = (float)(random.NextDouble() - 0.5) * 60,
                VelocityY = (float)(random.NextDouble() * 80 + 40),
                Color = color,
                Size = random.Next(4, 9),
                LifeSeconds = GameConstants.CelebrationParticleDuration
            });
        }
    }

    /// <summary>
    /// Processes game events and creates appropriate visual effects.
    /// </summary>
    public void HandleEvents(IReadOnlyList<GameEvent> events, Position ratPosition)
    {
        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case GameEventKind.ShotWaveFired:
                    HandleShotWaveFired(e);
                    break;
                    
                case GameEventKind.ShotHit:
                    HandleShotHit(e);
                    break;
                    
                case GameEventKind.SnakeBite:
                    HandleSnakeBite(e);
                    break;
                    
                case GameEventKind.GemFound:
                    HandleGemFound(e);
                    break;
                    
                case GameEventKind.HealthPickup:
                    HandleHealthPickup(e);
                    break;
                    
                case GameEventKind.ShieldPickup:
                    HandleShieldPickup(e);
                    break;
                    
                case GameEventKind.SpeedBoostPickup:
                    HandleSpeedBoostPickup(e);
                    break;
                    
                case GameEventKind.RockDug:
                    HandleRockDug(e);
                    break;
                    
                case GameEventKind.ComboMilestone:
                    HandleComboMilestone(e, ratPosition);
                    break;
                    
                case GameEventKind.ShotsAvoided:
                    HandleShotsAvoided(e, ratPosition);
                    break;
            }
        }
    }

    private void HandleShotWaveFired(GameEvent e)
    {
        if (e.Positions != null)
        {
            foreach (var target in e.Positions)
                AddEffect(VisualEffectKind.ShotImpact, target, 0.3f);
        }
    }

    private void HandleShotHit(GameEvent e)
    {
        TriggerRatHitFlash(GameConstants.RatHitFlashDuration);
        StartShake(0.25, 10);
        if (e.Position is { } pos)
            SpawnFloatingText(pos, $"-{e.Amount}", GameColors.Shot, _shakeOffset);
    }

    private void HandleSnakeBite(GameEvent e)
    {
        if (e.Position is { } pos)
        {
            AddEffect(VisualEffectKind.SnakeBite, pos, 0.35f);
            SpawnFloatingText(pos, "-1", GameColors.Shot, _shakeOffset);
            AddConsumedItem(pos, CellContent.Snake, GameConstants.ConsumedItemDuration);
        }
        TriggerRatHitFlash(GameConstants.SnakeBiteFlashDuration);
        StartShake(0.3, 10);
    }

    private void HandleGemFound(GameEvent e)
    {
        if (e.Position is { } pos)
        {
            AddEffect(VisualEffectKind.GemSparkle, pos, 1.0f);
            SpawnFloatingText(pos, "+100", GameColors.Gold, _shakeOffset);
            AddConsumedItem(pos, CellContent.Gem, GameConstants.GemConsumedDuration);
        }
    }

    private void HandleHealthPickup(GameEvent e)
    {
        if (e.Position is { } pos)
        {
            AddEffect(VisualEffectKind.HealthPickup, pos, 0.5f);
            SpawnFloatingText(pos, "+HP", GameColors.Success, _shakeOffset);
            AddConsumedItem(pos, CellContent.HealthPickup, GameConstants.ConsumedItemDuration);
        }
    }

    private void HandleShieldPickup(GameEvent e)
    {
        if (e.Position is { } pos)
        {
            AddEffect(VisualEffectKind.ShieldPickup, pos, 0.6f);
            SpawnFloatingText(pos, "SHIELD!", GameColors.Cyan, _shakeOffset);
            AddConsumedItem(pos, CellContent.Shield, GameConstants.ConsumedItemDuration);
        }
    }

    private void HandleSpeedBoostPickup(GameEvent e)
    {
        if (e.Position is { } pos)
        {
            AddEffect(VisualEffectKind.SpeedBoostPickup, pos, 0.5f);
            SpawnFloatingText(pos, "SPEED!", GameColors.Gold, _shakeOffset);
            AddConsumedItem(pos, CellContent.SpeedBoost, GameConstants.ConsumedItemDuration);
        }
    }

    private void HandleRockDug(GameEvent e)
    {
        if (e.Position is { } pos)
        {
            AddEffect(VisualEffectKind.RockDug, pos, 0.45f);
            SpawnFloatingText(pos, "DIG!", GameColors.HudDim, _shakeOffset);
        }
        StartShake(0.18, 6);
    }

    private void HandleComboMilestone(GameEvent e, Position ratPosition)
    {
        AddEffect(VisualEffectKind.ComboFlash, ratPosition, 0.6f);
        SpawnFloatingText(ratPosition, $"x{e.Amount}!", GameColors.Gold, _shakeOffset);
    }

    private void HandleShotsAvoided(GameEvent e, Position ratPosition)
    {
        if (e.Amount > 0)
            SpawnFloatingText(ratPosition, $"+{e.Amount * 5}", new Color(150, 200, 150, 255), _shakeOffset);
    }
}
