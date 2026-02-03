using System.Collections.Generic;

namespace Rat.Game;

public sealed class LevelGenerator
{
    public Level Generate(ChapterSettings settings, IRng rng)
    {
        const int maxAttempts = 250;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var level = TryGenerateOnce(settings, rng);
            if (level is not null)
                return level;
        }

        throw new InvalidOperationException("Failed to generate a solvable level after many attempts.");
    }

    private static Level? TryGenerateOnce(ChapterSettings settings, IRng rng)
    {
        var width = settings.Width;
        var height = settings.Height;

        var start = new Position(rng.Next(0, width), rng.Next(0, height));
        var gem = PickGemPosition(rng, width, height, start);
        var safeRadius = settings.ChapterNumber <= 2 ? 1 : 0;

        var contents = new CellContent[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pos = new Position(x, y);
                if (pos == start)
                {
                    contents[y, x] = CellContent.Empty;
                    continue;
                }

                if (pos == gem)
                {
                    contents[y, x] = CellContent.Gem;
                    continue;
                }

                if (safeRadius > 0 && ManhattanDistance(pos, start) <= safeRadius)
                {
                    contents[y, x] = CellContent.Empty;
                    continue;
                }

                var roll = rng.NextDouble();
                var cumulative = 0.0;
                
                cumulative += settings.RockChance;
                if (roll < cumulative)
                {
                    contents[y, x] = CellContent.Rock;
                    continue;
                }
                
                cumulative += settings.SnakeChance;
                if (roll < cumulative)
                {
                    contents[y, x] = CellContent.Snake;
                    continue;
                }
                
                cumulative += settings.HealthPickupChance;
                if (roll < cumulative)
                {
                    contents[y, x] = CellContent.HealthPickup;
                    continue;
                }
                
                cumulative += settings.ShieldChance;
                if (roll < cumulative)
                {
                    contents[y, x] = CellContent.Shield;
                    continue;
                }
                
                cumulative += settings.SpeedBoostChance;
                if (roll < cumulative)
                {
                    contents[y, x] = CellContent.SpeedBoost;
                    continue;
                }
                
                contents[y, x] = CellContent.Empty;
            }
        }

        EnsureMinimumPieces(contents, start, gem, safeRadius, settings.ChapterNumber, rng);

        if (!IsReachable(contents, start, gem))
            return null;

        var cells = new Cell[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                cells[y, x] = new Cell(contents[y, x]);
        }

        cells[start.Y, start.X].IsDug = true;

        return new Level(width, height, start, gem, cells);
    }

    private static Position PickGemPosition(IRng rng, int width, int height, Position start)
    {
        var maxDistance = (width - 1) + (height - 1);
        var desired = Math.Clamp((width + height) / 3, 4, 12);
        var minDistance = Math.Min(desired, maxDistance);

        var candidates = new List<Position>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pos = new Position(x, y);
                if (pos == start)
                    continue;

                if (ManhattanDistance(pos, start) >= minDistance)
                    candidates.Add(pos);
            }
        }

        if (candidates.Count == 0)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = new Position(x, y);
                    if (pos != start)
                        candidates.Add(pos);
                }
            }
        }

        return candidates[rng.Next(0, candidates.Count)];
    }

    private static void EnsureMinimumPieces(
        CellContent[,] contents,
        Position start,
        Position gem,
        int safeRadius,
        int chapterNumber,
        IRng rng)
    {
        if (chapterNumber < 2)
            return;

        var height = contents.GetLength(0);
        var width = contents.GetLength(1);

        var hasRock = false;
        var hasSnake = false;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (contents[y, x] == CellContent.Rock) hasRock = true;
                if (contents[y, x] == CellContent.Snake) hasSnake = true;
            }
        }

        if (!hasRock)
            TryPlace(contents, start, gem, safeRadius, CellContent.Rock, rng);

        if (chapterNumber >= 3 && !hasSnake)
            TryPlace(contents, start, gem, safeRadius, CellContent.Snake, rng);
    }

    private static void TryPlace(
        CellContent[,] contents,
        Position start,
        Position gem,
        int safeRadius,
        CellContent content,
        IRng rng)
    {
        var height = contents.GetLength(0);
        var width = contents.GetLength(1);

        var candidates = new List<Position>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pos = new Position(x, y);
                if (pos == start || pos == gem)
                    continue;

                if (safeRadius > 0 && ManhattanDistance(pos, start) <= safeRadius)
                    continue;

                if (contents[y, x] != CellContent.Empty)
                    continue;

                candidates.Add(pos);
            }
        }

        if (candidates.Count == 0)
            return;

        var chosen = candidates[rng.Next(0, candidates.Count)];
        contents[chosen.Y, chosen.X] = content;
    }

    private static int ManhattanDistance(Position a, Position b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static bool IsReachable(CellContent[,] contents, Position start, Position goal)
    {
        var height = contents.GetLength(0);
        var width = contents.GetLength(1);

        var visited = new bool[height, width];
        var queue = new Queue<Position>();

        visited[start.Y, start.X] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
                return true;

            foreach (var next in GetNeighbors(current, width, height))
            {
                if (visited[next.Y, next.X])
                    continue;

                if (contents[next.Y, next.X] == CellContent.Rock)
                    continue;

                visited[next.Y, next.X] = true;
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static IEnumerable<Position> GetNeighbors(Position p, int width, int height)
    {
        if (p.Y > 0) yield return new Position(p.X, p.Y - 1);
        if (p.Y < height - 1) yield return new Position(p.X, p.Y + 1);
        if (p.X > 0) yield return new Position(p.X - 1, p.Y);
        if (p.X < width - 1) yield return new Position(p.X + 1, p.Y);
    }
}
