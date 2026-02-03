using Rat.Game;

namespace Rat.Cli;

public static class ConsoleRenderer
{
    public static void Render(GameSession session, IReadOnlyList<GameMessage> lastMessages, bool revealAll = false, bool clearScreen = true)
    {
        if (clearScreen)
            Console.Clear();

        Console.WriteLine("RAT — find the gem, avoid shots, dig through dust");
        var ratTargeted = session.TelegraphedShots.Any(s => s.Target == session.Rat.Position);
        Console.WriteLine(
            $"Chapter {session.ChapterNumber} | Health {session.Rat.Health}/{session.Rat.MaxHealth} | Shots/turn {session.ChapterSettings.ShotsPerTurn}" +
            (ratTargeted ? " | TARGETED!" : string.Empty));
        Console.WriteLine("Move: WASD/Arrows | Wait: Space/Enter | Quit: Q");
        Console.WriteLine("Legend: R=Rat  X=Incoming shot  ▒=Dust  ·=Dug  █=Rock  G=Gem");
        Console.WriteLine();

        WriteGrid(session, revealAll);

        if (lastMessages.Count > 0)
        {
            Console.WriteLine();
            foreach (var message in lastMessages.TakeLast(6))
                WriteMessage(message);
        }

        Console.WriteLine();

        switch (session.Status)
        {
            case SessionStatus.InProgress:
                Console.WriteLine("Incoming shots are marked with X. They land after you move.");
                break;
            case SessionStatus.ChapterComplete:
                Console.WriteLine("Chapter complete! Press N/Enter for next chapter, or Q to quit.");
                break;
            case SessionStatus.GameOver:
                Console.WriteLine("Game over. Press R to restart, or Q to quit.");
                break;
            default:
                break;
        }
    }

    private static void WriteGrid(GameSession session, bool revealAll)
    {
        var level = session.Level;
        var shotTargets = session.TelegraphedShots.Select(s => s.Target).ToHashSet();
        Console.WriteLine($"+{new string('-', level.Width)}+");

        for (var y = 0; y < level.Height; y++)
        {
            Console.Write('|');

            for (var x = 0; x < level.Width; x++)
            {
                var pos = new Position(x, y);

                var isRat = session.Rat.Position == pos;
                var isTargeted = shotTargets.Contains(pos);

                var originalFg = Console.ForegroundColor;
                if (isTargeted)
                    Console.ForegroundColor = ConsoleColor.Red;

                if (isRat)
                {
                    Console.Write('R');
                    Console.ForegroundColor = originalFg;
                    continue;
                }

                if (isTargeted)
                {
                    Console.Write('X');
                    Console.ForegroundColor = originalFg;
                    continue;
                }

                var cell = level.GetCell(pos);
                if (!revealAll && !cell.IsDug)
                {
                    Console.Write('▒');
                    Console.ForegroundColor = originalFg;
                    continue;
                }

                Console.Write(cell.Content switch
                {
                    CellContent.Empty => '·',
                    CellContent.Rock => '█',
                    CellContent.Snake => 's',
                    CellContent.Gem => 'G',
                    _ => '?',
                });

                Console.ForegroundColor = originalFg;
            }

            Console.WriteLine('|');
        }

        Console.WriteLine($"+{new string('-', level.Width)}+");
    }

    private static void WriteMessage(GameMessage message)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = message.Kind switch
        {
            GameMessageKind.Success => ConsoleColor.Green,
            GameMessageKind.Damage => ConsoleColor.Red,
            GameMessageKind.Warning => ConsoleColor.Yellow,
            _ => original,
        };

        Console.WriteLine($"- {message.Text}");
        Console.ForegroundColor = original;
    }
}
