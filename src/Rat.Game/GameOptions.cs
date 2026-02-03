namespace Rat.Game;

public sealed record GameOptions
{
    public int MaxHealth { get; init; } = 3;

    public int MinWidth { get; init; } = 10;
    public int MinHeight { get; init; } = 8;
    public int MaxWidth { get; init; } = 36;
    public int MaxHeight { get; init; } = 18;
}

