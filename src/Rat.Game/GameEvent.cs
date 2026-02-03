using System.Collections.Generic;

namespace Rat.Game;

public enum GameEventKind
{
    Moved,
    Dug,
    BumpedWall,
    BlockedByRock,
    SnakeBite,
    ShotWaveFired,
    ShotHit,
    ShotsAvoided,
    GemFound,
    HealthPickup,
    ShieldPickup,
    SpeedBoostPickup,
    RockDug,
    ComboMilestone,
    ChapterBonus,
}

public sealed record GameEvent(
    GameEventKind Kind,
    Position? Position = null,
    int Amount = 0,
    IReadOnlyList<Position>? Positions = null);

