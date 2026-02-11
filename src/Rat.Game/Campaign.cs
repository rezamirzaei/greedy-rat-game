namespace Rat.Game;

public sealed class Campaign
{
    private readonly GameOptions _options;

    public Campaign(GameOptions options)
    {
        _options = options;
    }

    public ChapterSettings GetSettings(int chapterNumber)
    {
        if (chapterNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(chapterNumber), chapterNumber, "Chapter must be >= 1.");

        var chapterIndex = chapterNumber - 1;

        // Grid size grows gradually - starts cozy, becomes more challenging
        var width = Math.Clamp(_options.MinWidth + chapterIndex, _options.MinWidth, _options.MaxWidth);
        var height = Math.Clamp(_options.MinHeight + (chapterIndex / 2), _options.MinHeight, _options.MaxHeight);

        // Balanced obstacle distribution:
        // - Rocks provide strategic obstacles (8-20%)
        // - Snakes are rare but dangerous (2-8% max, starting lower)
        var rockChance = Math.Min(0.20, 0.08 + chapterIndex * 0.012);
        var snakeChance = Math.Min(0.08, 0.02 + chapterIndex * 0.006); // Much lower snake density!
        
        // Power-ups scale with difficulty to maintain fairness
        var healthPickupChance = Math.Min(0.10, 0.04 + chapterIndex * 0.006);
        var shieldChance = Math.Min(0.07, 0.02 + chapterIndex * 0.005);
        var speedBoostChance = Math.Min(0.06, 0.02 + chapterIndex * 0.004);

        // Shots scale gradually - 1 shot at start, up to 5 max
        var shotsPerTurn = Math.Min(5, 1 + (chapterIndex / 3));
        var shotAccuracy = Math.Min(0.80, 0.20 + chapterIndex * 0.04);
        var shotRadius = Math.Max(3, 7 - (chapterIndex / 3));
        
        // Extra rock digs help in harder chapters
        var bonusRockDigs = chapterIndex >= 3 ? 1 + (chapterIndex / 4) : 0;

        return new ChapterSettings(
            ChapterNumber: chapterNumber,
            Width: width,
            Height: height,
            RockChance: rockChance,
            SnakeChance: snakeChance,
            HealthPickupChance: healthPickupChance,
            ShieldChance: shieldChance,
            SpeedBoostChance: speedBoostChance,
            ShotsPerTurn: shotsPerTurn,
            ShotAccuracy: shotAccuracy,
            ShotRadius: shotRadius,
            BonusRockDigs: bonusRockDigs);
    }
}
