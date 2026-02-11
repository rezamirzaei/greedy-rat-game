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

    /// <summary>
    /// Gets the Manhattan distance from a position to the gem.
    /// </summary>
    public int GetDistanceToGem(Position position) =>
        Math.Abs(position.X - GemPosition.X) + Math.Abs(position.Y - GemPosition.Y);

    /// <summary>
    /// Gets a hint about which direction the gem is relative to a position.
    /// Returns (horizontalHint, verticalHint) where:
    /// -1 = gem is left/up, 0 = same column/row, 1 = gem is right/down
    /// </summary>
    public (int horizontal, int vertical) GetGemDirectionHint(Position position)
    {
        var horizontal = GemPosition.X.CompareTo(position.X);
        var vertical = GemPosition.Y.CompareTo(position.Y);
        return (horizontal, vertical);
    }

    /// <summary>
    /// Gets a "temperature" reading (0.0 = cold/far, 1.0 = hot/close).
    /// </summary>
    public float GetGemTemperature(Position position)
    {
        var maxDistance = Width + Height - 2;
        if (maxDistance <= 0) return 1f;
        var distance = GetDistanceToGem(position);
        return 1f - (float)distance / maxDistance;
    }
}
