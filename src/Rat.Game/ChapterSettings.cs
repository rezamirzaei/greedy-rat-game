namespace Rat.Game;

public sealed record ChapterSettings(
    int ChapterNumber,
    int Width,
    int Height,
    double RockChance,
    double SnakeChance,
    double HealthPickupChance,
    double ShieldChance,
    double SpeedBoostChance,
    int ShotsPerTurn,
    double ShotAccuracy,
    int ShotRadius,
    int BonusRockDigs);
