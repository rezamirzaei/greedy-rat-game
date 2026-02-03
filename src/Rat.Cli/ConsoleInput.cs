using Rat.Game;

namespace Rat.Cli;

public static class ConsoleInput
{
    public static bool TryReadCommand(out GameCommand command, out bool quitRequested)
    {
        quitRequested = false;

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            var key = keyInfo.Key;

            switch (key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    command = GameCommand.Move(Direction.Up);
                    return true;
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    command = GameCommand.Move(Direction.Down);
                    return true;
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    command = GameCommand.Move(Direction.Left);
                    return true;
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    command = GameCommand.Move(Direction.Right);
                    return true;
                case ConsoleKey.Spacebar:
                case ConsoleKey.Enter:
                    command = GameCommand.None;
                    return true;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    command = GameCommand.None;
                    quitRequested = true;
                    return false;
                default:
                    continue;
            }
        }
    }
}

