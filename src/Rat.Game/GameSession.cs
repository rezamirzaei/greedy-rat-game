using System.Collections.Generic;

namespace Rat.Game;

public sealed class GameSession
{
    private readonly Campaign _campaign;
    private readonly LevelGenerator _levelGenerator;
    private readonly Shooter _shooter;
    private readonly IRng _rng;
    private readonly List<Shot> _telegraphedShots = new();

    public GameSession(GameOptions? options = null, int? seed = null)
    {
        Options = options ?? new GameOptions();

        _rng = new SystemRng(seed);
        _campaign = new Campaign(Options);
        _levelGenerator = new LevelGenerator();
        _shooter = new Shooter(_rng);

        Rat = new Rat(Options.MaxHealth);
        StartChapter(1);
    }

    public GameOptions Options { get; }
    public int ChapterNumber { get; private set; }
    public ChapterSettings ChapterSettings { get; private set; } = null!;
    public Level Level { get; private set; } = null!;
    public Rat Rat { get; }
    public SessionStatus Status { get; private set; }
    public IReadOnlyList<Shot> TelegraphedShots => _telegraphedShots;

    public void StartChapter(int chapterNumber)
    {
        ChapterNumber = chapterNumber;
        ChapterSettings = _campaign.GetSettings(chapterNumber);
        Level = _levelGenerator.Generate(ChapterSettings, _rng);

        Rat.Reset(Level.Start);

        Status = SessionStatus.InProgress;
        _telegraphedShots.Clear();
        _telegraphedShots.AddRange(_shooter.GenerateTelegraphedShots(Level, Rat.Position, ChapterSettings));
    }

    public void StartNextChapter()
    {
        if (Status != SessionStatus.ChapterComplete)
            throw new InvalidOperationException("Cannot start next chapter unless the current chapter is complete.");

        StartChapter(ChapterNumber + 1);
    }

    public TurnOutcome Advance(GameCommand command)
    {
        if (Status != SessionStatus.InProgress)
            return new TurnOutcome(Status, Array.Empty<GameMessage>(), Array.Empty<GameEvent>());

        var messages = new List<GameMessage>();
        var events = new List<GameEvent>();

        if (command.MoveDirection is Direction direction)
            ProcessMove(direction, messages, events);

        if (Status == SessionStatus.ChapterComplete)
            return new TurnOutcome(Status, messages, events);

        ResolveTelegraphedShots(messages, events);

        if (!Rat.IsAlive)
        {
            Status = SessionStatus.GameOver;
            _telegraphedShots.Clear();
            messages.Add(new GameMessage(GameMessageKind.Damage, "The rat was defeated. Game over."));
            return new TurnOutcome(Status, messages, events);
        }

        _telegraphedShots.Clear();
        _telegraphedShots.AddRange(_shooter.GenerateTelegraphedShots(Level, Rat.Position, ChapterSettings));

        return new TurnOutcome(Status, messages, events);
    }

    private void ProcessMove(Direction direction, List<GameMessage> messages, List<GameEvent> events)
    {
        var next = Rat.Position.Offset(direction.ToDelta());
        if (!Level.InBounds(next))
        {
            messages.Add(new GameMessage(GameMessageKind.Warning, "Bump! You hit the wall."));
            events.Add(new GameEvent(GameEventKind.BumpedWall, Position: Rat.Position));
            return;
        }

        var cell = Level.GetCell(next);
        cell.IsDug = true;
        events.Add(new GameEvent(GameEventKind.Dug, Position: next));

        if (cell.Content == CellContent.Rock)
        {
            messages.Add(new GameMessage(GameMessageKind.Warning, "A rock blocks the way."));
            events.Add(new GameEvent(GameEventKind.BlockedByRock, Position: next));
            return;
        }

        Rat.MoveTo(next);
        events.Add(new GameEvent(GameEventKind.Moved, Position: next));

        switch (cell.Content)
        {
            case CellContent.Empty:
                return;
            case CellContent.Snake:
                Rat.Damage(1);
                cell.Content = CellContent.Empty;
                messages.Add(new GameMessage(GameMessageKind.Damage, "A snake bites you! -1 health."));
                events.Add(new GameEvent(GameEventKind.SnakeBite, Position: next, Amount: 1));
                break;
            case CellContent.Gem:
                Status = SessionStatus.ChapterComplete;
                messages.Add(new GameMessage(GameMessageKind.Success, "You found the gem! Chapter complete."));
                events.Add(new GameEvent(GameEventKind.GemFound, Position: next));
                _telegraphedShots.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cell.Content), cell.Content, "Unknown cell content.");
        }

        if (!Rat.IsAlive)
        {
            Status = SessionStatus.GameOver;
            _telegraphedShots.Clear();
        }
    }

    private void ResolveTelegraphedShots(List<GameMessage> messages, List<GameEvent> events)
    {
        if (_telegraphedShots.Count == 0)
            return;

        events.Add(new GameEvent(
            GameEventKind.ShotWaveFired,
            Positions: _telegraphedShots.Select(s => s.Target).ToArray()));

        var hits = 0;
        for (var i = 0; i < _telegraphedShots.Count; i++)
        {
            if (_telegraphedShots[i].Target == Rat.Position)
                hits++;
        }

        if (hits <= 0)
            return;

        Rat.Damage(hits);
        messages.Add(new GameMessage(GameMessageKind.Damage, hits == 1 ? "You got shot! -1 health." : $"You got shot {hits} times! -{hits} health."));
        events.Add(new GameEvent(GameEventKind.ShotHit, Position: Rat.Position, Amount: hits));
    }
}
