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

        var width = Math.Clamp(_options.MinWidth + chapterIndex * 2, _options.MinWidth, _options.MaxWidth);
        var height = Math.Clamp(_options.MinHeight + chapterIndex, _options.MinHeight, _options.MaxHeight);

        var rockChance = Math.Min(0.28, 0.08 + chapterIndex * 0.015);
        var snakeChance = Math.Min(0.20, 0.03 + chapterIndex * 0.012);
        
        // Power-ups become more common in harder chapters to balance difficulty
        var healthPickupChance = Math.Min(0.08, 0.02 + chapterIndex * 0.005);
        var shieldChance = Math.Min(0.06, 0.01 + chapterIndex * 0.004);
        var speedBoostChance = Math.Min(0.05, 0.01 + chapterIndex * 0.003);

        var shotsPerTurn = Math.Min(6, 1 + (chapterIndex / 2));
        var shotAccuracy = Math.Min(0.90, 0.25 + chapterIndex * 0.05);
        var shotRadius = Math.Max(2, 8 - (chapterIndex / 2));
        
        // Extra rock digs for harder chapters
        var bonusRockDigs = chapterIndex >= 4 ? 1 : 0;

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
