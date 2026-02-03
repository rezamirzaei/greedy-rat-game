namespace Rat.Game;

public enum GameMessageKind
{
    Info,
    Warning,
    Damage,
    Success,
    Bonus,
    PowerUp,
}

public sealed record GameMessage(GameMessageKind Kind, string Text);

