namespace Rat.Game;

public sealed record TurnOutcome(
    SessionStatus Status,
    IReadOnlyList<GameMessage> Messages,
    IReadOnlyList<GameEvent> Events);
