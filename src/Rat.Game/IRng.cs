namespace Rat.Game;

public interface IRng
{
    int Next(int minInclusive, int maxExclusive);
    double NextDouble();
}

public sealed class SystemRng : IRng
{
    private readonly Random _random;

    public SystemRng(int? seed = null)
    {
        _random = seed is null ? new Random() : new Random(seed.Value);
    }

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    public double NextDouble() => _random.NextDouble();
}

