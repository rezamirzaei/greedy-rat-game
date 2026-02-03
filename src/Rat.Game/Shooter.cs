using System.Collections.Generic;

namespace Rat.Game;

public sealed class Shooter
{
    private readonly IRng _rng;

    public Shooter(IRng rng)
    {
        _rng = rng;
    }

    public IReadOnlyList<Shot> GenerateTelegraphedShots(Level level, Position ratPosition, ChapterSettings settings)
    {
        var uniqueTargets = new HashSet<Position>();
        var maxAttempts = Math.Max(50, settings.ShotsPerTurn * 30);

        for (var attempt = 0; attempt < maxAttempts && uniqueTargets.Count < settings.ShotsPerTurn; attempt++)
        {
            var preferNearRat = _rng.NextDouble() < settings.ShotAccuracy;
            var target = preferNearRat
                ? RandomNear(level, ratPosition, settings.ShotRadius)
                : RandomAnywhere(level);

            uniqueTargets.Add(target);
        }

        var shots = new Shot[uniqueTargets.Count];
        var i = 0;
        foreach (var target in uniqueTargets)
            shots[i++] = new Shot(target);

        return shots;
    }

    private Position RandomAnywhere(Level level) =>
        new Position(_rng.Next(0, level.Width), _rng.Next(0, level.Height));

    private Position RandomNear(Level level, Position center, int radius)
    {
        var minX = Math.Max(0, center.X - radius);
        var maxX = Math.Min(level.Width - 1, center.X + radius);
        var minY = Math.Max(0, center.Y - radius);
        var maxY = Math.Min(level.Height - 1, center.Y + radius);

        var x = _rng.Next(minX, maxX + 1);
        var y = _rng.Next(minY, maxY + 1);
        return new Position(x, y);
    }
}

