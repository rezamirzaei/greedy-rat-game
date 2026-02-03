namespace Rat.Desktop;

internal static class Program
{
    private static int Main(string[] args)
    {
        var seed = TryParseSeed(args);
        new RatDesktopApp(seed).Run();
        return 0;
    }

    private static int? TryParseSeed(string[] args)
    {
        // Usage: --seed 123
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--seed", StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(args[i + 1], out var seed))
                return seed;
        }

        return null;
    }
}

