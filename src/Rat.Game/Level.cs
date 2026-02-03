namespace Rat.Game;

public sealed class Level
{
    private readonly Cell[,] _cells;

    public Level(int width, int height, Position start, Position gemPosition, Cell[,] cells)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        if (cells.GetLength(0) != height || cells.GetLength(1) != width)
            throw new ArgumentException("Cell array dimensions must match width/height.", nameof(cells));

        Width = width;
        Height = height;
        Start = start;
        GemPosition = gemPosition;
        _cells = cells;
    }

    public int Width { get; }
    public int Height { get; }
    public Position Start { get; }
    public Position GemPosition { get; }

    public bool InBounds(Position position) =>
        position.X >= 0 && position.X < Width &&
        position.Y >= 0 && position.Y < Height;

    public Cell GetCell(Position position)
    {
        if (!InBounds(position))
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position out of bounds.");

        return _cells[position.Y, position.X];
    }
}

