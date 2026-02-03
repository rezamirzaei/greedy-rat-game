namespace Rat.Game;

public readonly record struct GameCommand(Direction? MoveDirection)
{
    public static GameCommand None => new GameCommand(null);

    public static GameCommand Move(Direction direction) => new GameCommand(direction);
}

