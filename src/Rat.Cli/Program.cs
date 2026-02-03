using Rat.Game;

namespace Rat.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var seed = TryParseSeed(args);

        if (args.Any(a => string.Equals(a, "--demo", StringComparison.OrdinalIgnoreCase)))
            return RunDemo(args, seed);

        var session = new GameSession(seed: seed);

        IReadOnlyList<GameMessage> lastMessages = Array.Empty<GameMessage>();

        var cursorVisibilitySupported = OperatingSystem.IsWindows();
        var originalCursorVisible = true;
        if (cursorVisibilitySupported)
        {
            originalCursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;
        }

        try
        {
            while (true)
            {
                var revealAll = session.Status is SessionStatus.ChapterComplete or SessionStatus.GameOver;
                ConsoleRenderer.Render(session, lastMessages, revealAll: revealAll);

                if (session.Status == SessionStatus.GameOver)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    if (key is ConsoleKey.Q or ConsoleKey.Escape)
                        return 0;

                    if (key == ConsoleKey.R)
                    {
                        session = new GameSession(seed: seed);
                        lastMessages = Array.Empty<GameMessage>();
                    }

                    continue;
                }

                if (session.Status == SessionStatus.ChapterComplete)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    if (key is ConsoleKey.Q or ConsoleKey.Escape)
                        return 0;

                    if (key is ConsoleKey.N or ConsoleKey.Enter or ConsoleKey.Spacebar)
                    {
                        session.StartNextChapter();
                        lastMessages = Array.Empty<GameMessage>();
                    }

                    continue;
                }

                if (!ConsoleInput.TryReadCommand(out var command, out var quitRequested) && quitRequested)
                    return 0;

                var outcome = session.Advance(command);
                lastMessages = outcome.Messages;
            }
        }
        finally
        {
            if (cursorVisibilitySupported)
                Console.CursorVisible = originalCursorVisible;
            Console.ResetColor();
        }
    }

    private static int RunDemo(string[] args, int? seed)
    {
        var steps = TryParseIntOption(args, "--steps") ?? 4;
        steps = Math.Clamp(steps, 1, 20);

        var session = new GameSession(seed: seed);
        IReadOnlyList<GameMessage> lastMessages = Array.Empty<GameMessage>();

        Console.WriteLine("RAT — DEMO MODE");
        Console.WriteLine();

        ConsoleRenderer.Render(session, lastMessages, clearScreen: false);

        for (var i = 0; i < steps; i++)
        {
            if (session.Status != SessionStatus.InProgress)
                break;

            var command = PickDemoCommand(session, i);
            Console.WriteLine();
            Console.WriteLine($"Turn {i + 1}: {Describe(command)}");

            var outcome = session.Advance(command);
            lastMessages = outcome.Messages;

            var revealAll = session.Status is SessionStatus.ChapterComplete or SessionStatus.GameOver;
            ConsoleRenderer.Render(session, lastMessages, revealAll: revealAll, clearScreen: false);
        }

        Console.WriteLine();
        Console.WriteLine("Demo finished. Run without --demo to play interactively.");
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

    private static int? TryParseIntOption(string[] args, string optionName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(args[i + 1], out var value))
                return value;
        }

        return null;
    }

    private static string Describe(GameCommand command) =>
        command.MoveDirection is null ? "Wait" : $"Move {command.MoveDirection.Value}";

    private static GameCommand PickDemoCommand(GameSession session, int turnIndex)
    {
        var pos = session.Rat.Position;
        var level = session.Level;

        // Avoid walls so the demo actually moves.
        if (pos.X <= 0) return GameCommand.Move(Direction.Right);
        if (pos.X >= level.Width - 1) return GameCommand.Move(Direction.Left);
        if (pos.Y <= 0) return GameCommand.Move(Direction.Down);
        if (pos.Y >= level.Height - 1) return GameCommand.Move(Direction.Up);

        return (turnIndex % 4) switch
        {
            0 => GameCommand.Move(Direction.Right),
            1 => GameCommand.Move(Direction.Down),
            2 => GameCommand.Move(Direction.Left),
            _ => GameCommand.None,
        };
    }
}
