namespace Rat.Game;

public sealed class RealTimeGameSession
{
    private readonly Campaign _campaign;
    private readonly LevelGenerator _levelGenerator;
    private readonly Shooter _shooter;
    private readonly IRng _rng;
    private readonly List<Shot> _telegraphedShots = new();

    private double _timeUntilShotsFireSeconds;
    private double _moveCooldownSeconds;
    
    private const double ShieldDuration = 4.0;
    private const double SpeedBoostDuration = 3.0;
    private const double BaseMoveCooldown = 0.11;
    private const double SpeedBoostMoveCooldown = 0.06;

    public RealTimeGameSession(GameOptions? options = null, int? seed = null)
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
    public double TimeUntilShotsFireSeconds => _timeUntilShotsFireSeconds;
    public double ShotTelegraphSeconds { get; private set; }
    public double MoveCooldownSeconds { get; private set; }

    public void StartChapter(int chapterNumber)
    {
        ChapterNumber = chapterNumber;
        ChapterSettings = _campaign.GetSettings(chapterNumber);
        Level = _levelGenerator.Generate(ChapterSettings, _rng);

        Rat.Reset(Level.Start);
        Rat.AddRockDigs(ChapterSettings.BonusRockDigs);
        Status = SessionStatus.InProgress;

        ShotTelegraphSeconds = CalculateShotTelegraphSeconds(chapterNumber);
        MoveCooldownSeconds = BaseMoveCooldown;

        _moveCooldownSeconds = 0;
        StartNewShotWave();
    }

    public void StartNextChapter()
    {
        if (Status != SessionStatus.ChapterComplete)
            throw new InvalidOperationException("Cannot start next chapter unless the current chapter is complete.");

        StartChapter(ChapterNumber + 1);
    }

    public TurnOutcome Update(double deltaSeconds, Direction? moveDirection)
    {
        if (deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Delta seconds must be >= 0.");

        if (Status != SessionStatus.InProgress)
            return new TurnOutcome(Status, Array.Empty<GameMessage>(), Array.Empty<GameEvent>());

        var messages = new List<GameMessage>();
        var events = new List<GameEvent>();

        // Update rat timers
        Rat.UpdateTimers(deltaSeconds);
        
        // Adjust move cooldown based on speed boost
        var currentMoveCooldown = Rat.HasSpeedBoost ? SpeedBoostMoveCooldown : BaseMoveCooldown;

        _moveCooldownSeconds = Math.Max(0, _moveCooldownSeconds - deltaSeconds);
        if (moveDirection is Direction direction && _moveCooldownSeconds <= 0)
        {
            ProcessMove(direction, messages, events);
            _moveCooldownSeconds = currentMoveCooldown;
        }

        if (Status == SessionStatus.ChapterComplete)
        {
            // Award chapter completion bonus
            var chapterBonus = 50 * ChapterNumber;
            var healthBonus = Rat.Health * 25;
            var comboBonus = Rat.ComboCount * 5;
            var totalBonus = chapterBonus + healthBonus + comboBonus;
            Rat.AddScore(totalBonus);
            messages.Add(new GameMessage(GameMessageKind.Bonus, $"Chapter bonus: +{totalBonus} points!"));
            events.Add(new GameEvent(GameEventKind.ChapterBonus, Amount: totalBonus));
            return new TurnOutcome(Status, messages, events);
        }

        _timeUntilShotsFireSeconds -= deltaSeconds;

        var safetyIterations = 0;
        while (_timeUntilShotsFireSeconds <= 0 && Status == SessionStatus.InProgress)
        {
            safetyIterations++;
            if (safetyIterations > 5)
                break;

            events.Add(new GameEvent(
                GameEventKind.ShotWaveFired,
                Positions: _telegraphedShots.Select(s => s.Target).ToArray()));

            ResolveShotWave(messages, events);
            if (Status != SessionStatus.InProgress)
                break;

            StartNewShotWave();
        }

        return new TurnOutcome(Status, messages, events);
    }

    private void StartNewShotWave()
    {
        _telegraphedShots.Clear();
        _telegraphedShots.AddRange(_shooter.GenerateTelegraphedShots(Level, Rat.Position, ChapterSettings));
        _timeUntilShotsFireSeconds = ShotTelegraphSeconds;
    }

    private void ResolveShotWave(List<GameMessage> messages, List<GameEvent> events)
    {
        if (_telegraphedShots.Count == 0)
            return;

        var hits = 0;
        for (var i = 0; i < _telegraphedShots.Count; i++)
        {
            if (_telegraphedShots[i].Target == Rat.Position)
                hits++;
        }

        var avoided = _telegraphedShots.Count - hits;
        if (avoided > 0)
        {
            Rat.RecordShotAvoided(avoided);
            events.Add(new GameEvent(GameEventKind.ShotsAvoided, Amount: avoided));
            
            // Combo milestone notifications
            if (Rat.ComboCount >= 10 && (Rat.ComboCount - avoided) < 10)
            {
                messages.Add(new GameMessage(GameMessageKind.Bonus, $"Combo x{Rat.ComboCount}! Keep dodging!"));
                events.Add(new GameEvent(GameEventKind.ComboMilestone, Amount: Rat.ComboCount));
            }
            else if (Rat.ComboCount >= 25 && (Rat.ComboCount - avoided) < 25)
            {
                messages.Add(new GameMessage(GameMessageKind.Bonus, $"Amazing! Combo x{Rat.ComboCount}!"));
                events.Add(new GameEvent(GameEventKind.ComboMilestone, Amount: Rat.ComboCount));
            }
        }

        if (hits > 0)
        {
            var hadShield = Rat.HasShield;
            Rat.Damage(hits);
            
            if (hadShield)
            {
                messages.Add(new GameMessage(GameMessageKind.PowerUp, "Shield absorbed the damage!"));
            }
            else
            {
                messages.Add(new GameMessage(GameMessageKind.Damage, hits == 1 ? "You got shot! -1 health." : $"You got shot {hits} times! -{hits} health."));
                events.Add(new GameEvent(GameEventKind.ShotHit, Position: Rat.Position, Amount: hits));
            }
        }

        if (!Rat.IsAlive)
        {
            Status = SessionStatus.GameOver;
            _telegraphedShots.Clear();
            messages.Add(new GameMessage(GameMessageKind.Damage, $"The rat was defeated. Final score: {Rat.Score}"));
        }
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
            // Try to dig through rock if ability is available
            if (Rat.CanDigRock && Rat.UseRockDig())
            {
                cell.Content = CellContent.Empty;
                Rat.MoveTo(next);
                messages.Add(new GameMessage(GameMessageKind.Success, $"You dug through the rock! ({Rat.RockDigsRemaining} digs left)"));
                events.Add(new GameEvent(GameEventKind.RockDug, Position: next));
                events.Add(new GameEvent(GameEventKind.Moved, Position: next));
                Rat.AddScore(15);
                return;
            }
            
            messages.Add(new GameMessage(GameMessageKind.Warning, "A rock blocks the way."));
            events.Add(new GameEvent(GameEventKind.BlockedByRock, Position: next));
            return;
        }

        Rat.MoveTo(next);
        events.Add(new GameEvent(GameEventKind.Moved, Position: next));

        switch (cell.Content)
        {
            case CellContent.Empty:
                Rat.AddScore(1); // Small points for exploring
                return;
                
            case CellContent.Snake:
                Rat.Damage(1);
                cell.Content = CellContent.Empty;
                messages.Add(new GameMessage(GameMessageKind.Damage, "A snake bites you! -1 health."));
                events.Add(new GameEvent(GameEventKind.SnakeBite, Position: next, Amount: 1));
                break;
                
            case CellContent.Gem:
                Status = SessionStatus.ChapterComplete;
                Rat.CollectGem();
                messages.Add(new GameMessage(GameMessageKind.Success, "You found the gem! Chapter complete."));
                events.Add(new GameEvent(GameEventKind.GemFound, Position: next));
                _telegraphedShots.Clear();
                break;
                
            case CellContent.HealthPickup:
                Rat.Heal(1);
                cell.Content = CellContent.Empty;
                Rat.AddScore(20);
                messages.Add(new GameMessage(GameMessageKind.PowerUp, "Found a health pickup! +1 health."));
                events.Add(new GameEvent(GameEventKind.HealthPickup, Position: next));
                break;
                
            case CellContent.Shield:
                Rat.ActivateShield(ShieldDuration);
                cell.Content = CellContent.Empty;
                Rat.AddScore(25);
                messages.Add(new GameMessage(GameMessageKind.PowerUp, $"Shield activated for {ShieldDuration:0}s!"));
                events.Add(new GameEvent(GameEventKind.ShieldPickup, Position: next));
                break;
                
            case CellContent.SpeedBoost:
                Rat.ActivateSpeedBoost(SpeedBoostDuration);
                cell.Content = CellContent.Empty;
                Rat.AddScore(20);
                messages.Add(new GameMessage(GameMessageKind.PowerUp, $"Speed boost for {SpeedBoostDuration:0}s!"));
                events.Add(new GameEvent(GameEventKind.SpeedBoostPickup, Position: next));
                break;
                
            case CellContent.Rock:
                // Shouldn't reach here, but handle gracefully
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

    private static double CalculateShotTelegraphSeconds(int chapterNumber)
    {
        var chapterIndex = chapterNumber - 1;
        return Math.Clamp(1.35 - (chapterIndex * 0.06), 0.40, 1.35);
    }
}
