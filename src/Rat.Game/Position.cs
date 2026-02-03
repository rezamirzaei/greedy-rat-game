namespace Rat.Game;

public readonly record struct Position(int X, int Y)
{
    public Position Offset(Position delta) => new Position(X + delta.X, Y + delta.Y);
}

