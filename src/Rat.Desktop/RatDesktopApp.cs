using Rat.Game;
using Raylib_cs;
using System.Numerics;

namespace Rat.Desktop;

internal sealed class RatDesktopApp
{
    private const int TileSize = 34;
    private const int Margin = 16;
    private const int SidePanelWidth = 340;

	private readonly int? _seed;
	private RealTimeGameSession _session;
	private readonly List<GameMessage> _messageLog = new();
	private SpriteTextures? _sprites;
	private readonly List<VisualEffect> _effects = new();

	private double _elapsedSeconds;
	private bool _exitRequested;
	private double _shakeSecondsLeft;
	private float _shakeStrength = 6f;
	private float _ratHitFlashSecondsLeft;
	private Vector2 _shakeOffset;

	private static readonly Color Background = new(18, 18, 22, 255);
	private static readonly Color HudText = new(230, 230, 235, 255);
	private static readonly Color Dust = new(93, 76, 55, 255);
	private static readonly Color Dug = new(38, 38, 46, 255);
	private static readonly Color Border = new(22, 22, 28, 255);
	private static readonly Color Shot = new(225, 70, 70, 255);

    public RatDesktopApp(int? seed)
    {
        _seed = seed;
        _session = new RealTimeGameSession(seed: seed);
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1024, 768, "RAT");
        Raylib.SetTargetFPS(60);
        _sprites = new SpriteTextures();

        ResizeWindowToLevel();

        try
        {
            while (!Raylib.WindowShouldClose() && !_exitRequested)
            {
                var dt = Raylib.GetFrameTime();
                _elapsedSeconds += dt;

				var moveDirection = ReadMoveDirection();
				var outcome = _session.Update(dt, moveDirection);
				AppendMessages(outcome.Messages);
				HandleEvents(outcome.Events);
				UpdateEffects(dt);
				UpdateShake(dt);

				HandleMetaKeys();

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Background);
                DrawHud();
                DrawGrid();
                DrawMessages();
                DrawOverlayHints();
                Raylib.EndDrawing();
            }
        }
        finally
        {
            _sprites.Dispose();
            _sprites = null;
            Raylib.CloseWindow();
        }
    }

    private void HandleMetaKeys()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Q))
        {
            _exitRequested = true;
            return;
        }

        if (_session.Status == SessionStatus.InProgress)
            return;

		if (_session.Status == SessionStatus.ChapterComplete &&
			(Raylib.IsKeyPressed(KeyboardKey.N) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space)))
		{
			_session.StartNextChapter();
			_messageLog.Clear();
			_effects.Clear();
			ResizeWindowToLevel();
		}

		if (_session.Status == SessionStatus.GameOver && Raylib.IsKeyPressed(KeyboardKey.R))
		{
			_session = new RealTimeGameSession(seed: _seed);
			_messageLog.Clear();
			_effects.Clear();
			ResizeWindowToLevel();
		}
	}

    private void ResizeWindowToLevel()
    {
        var width = Margin * 3 + _session.Level.Width * TileSize + SidePanelWidth;
        var height = Margin * 2 + _session.Level.Height * TileSize;
        Raylib.SetWindowSize(width, height);
    }

    private static Direction? ReadMoveDirection()
    {
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up))
            return Direction.Up;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down))
            return Direction.Down;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left))
            return Direction.Left;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right))
            return Direction.Right;

        return null;
    }

    private void AppendMessages(IReadOnlyList<GameMessage> messages)
    {
        if (messages.Count == 0)
            return;

        foreach (var message in messages)
            _messageLog.Add(message);

        const int maxMessages = 6;
        while (_messageLog.Count > maxMessages)
            _messageLog.RemoveAt(0);
    }

	private void DrawHud()
	{
		if (_sprites is null)
			return;

		var panelX = Margin * 2 + _session.Level.Width * TileSize + (int)_shakeOffset.X;
		var x = panelX;
		var y = Margin + (int)_shakeOffset.Y;

		Raylib.DrawRectangle(panelX - Margin, y, SidePanelWidth + Margin, _session.Level.Height * TileSize, new Color(14, 14, 18, 255));
		Raylib.DrawRectangleLines(panelX - Margin, y, SidePanelWidth + Margin, _session.Level.Height * TileSize, Border);

        Raylib.DrawText("RAT", x, y, 30, HudText);

        y += 36;
        Raylib.DrawText($"Chapter {_session.ChapterNumber}", x, y, 20, HudText);
        
        // Score display
        Raylib.DrawText($"Score: {_session.Rat.Score}", x + 140, y, 20, new Color(255, 230, 80, 255));
        
        DrawHearts(x, y + 26);
        
        // Combo display
        if (_session.Rat.ComboCount > 0)
        {
            var comboColor = _session.Rat.ComboCount >= 10 
                ? new Color(255, 200, 100, 255) 
                : new Color(200, 200, 200, 255);
            Raylib.DrawText($"Combo: x{_session.Rat.ComboCount}", x + 140, y + 26, 16, comboColor);
        }

        y += 56;
        
        // Power-up status indicators
        var statusY = y;
        if (_session.Rat.HasShield)
        {
            var shieldColor = new Color(70, 130, 200, 255);
            Raylib.DrawText($"SHIELD: {_session.Rat.ShieldSecondsRemaining:0.0}s", x, statusY, 16, shieldColor);
            statusY += 18;
        }
        if (_session.Rat.HasSpeedBoost)
        {
            var speedColor = new Color(255, 230, 80, 255);
            Raylib.DrawText($"SPEED: {_session.Rat.SpeedBoostSecondsRemaining:0.0}s", x, statusY, 16, speedColor);
            statusY += 18;
        }
        if (_session.Rat.RockDigsRemaining > 0)
        {
            Raylib.DrawText($"Rock Digs: {_session.Rat.RockDigsRemaining}", x, statusY, 16, new Color(170, 170, 186, 255));
            statusY += 18;
        }
        
        y = Math.Max(y, statusY) + 4;
        
        Raylib.DrawText($"Shots per wave: {_session.ChapterSettings.ShotsPerTurn}", x, y, 18, HudText);

        var time = Math.Max(0, _session.TimeUntilShotsFireSeconds);
        Raylib.DrawText($"Next shots in {time:0.0}s", x, y + 22, 18, HudText);

        var barX = x;
        var barY = y + 48;
        var barW = SidePanelWidth - Margin * 2;
        var barH = 10;
        Raylib.DrawRectangle(barX, barY, barW, barH, Border);

        var t = _session.ShotTelegraphSeconds <= 0.0001 ? 0 : (float)(time / _session.ShotTelegraphSeconds);
        var fill = (int)(barW * Math.Clamp(t, 0f, 1f));
        Raylib.DrawRectangle(barX, barY, fill, barH, Shot);
    }

	private void DrawGrid()
	{
		if (_sprites is null)
			return;

		var gridOriginX = Margin + (int)_shakeOffset.X;
		var gridOriginY = Margin + (int)_shakeOffset.Y;

		var level = _session.Level;
		var revealAll = _session.Status is SessionStatus.ChapterComplete or SessionStatus.GameOver;
		var shotTargets = _session.TelegraphedShots.Select(s => s.Target).ToHashSet();
		var ratTargeted = shotTargets.Contains(_session.Rat.Position);

        var timeRatio = _session.ShotTelegraphSeconds <= 0.0001 ? 0f : (float)(_session.TimeUntilShotsFireSeconds / _session.ShotTelegraphSeconds);
        var urgency = 1f - Math.Clamp(timeRatio, 0f, 1f);
        var pulseSpeed = 3f + urgency * 10f;
        var pulse = 0.35f + 0.65f * MathF.Abs(MathF.Sin((float)_elapsedSeconds * pulseSpeed));

        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var pos = new Position(x, y);
                var cell = level.GetCell(pos);

                var px = gridOriginX + x * TileSize;
                var py = gridOriginY + y * TileSize;

				var baseColor = cell.IsDug
					? Dug
					: revealAll ? new Color(78, 66, 52, 255) : Dust;
				Raylib.DrawRectangle(px, py, TileSize, TileSize, baseColor);
				Raylib.DrawRectangleLines(px, py, TileSize, TileSize, Border);

				if (shotTargets.Contains(pos))
					DrawCrosshair(px, py, pulse);

				if (!cell.IsDug && !revealAll)
				{
					if (_session.Rat.Position == pos)
						DrawRat(px, py, ratTargeted);
					continue;
				}

				switch (cell.Content)
				{
					case CellContent.Rock:
						DrawSprite(_sprites.Rock, px, py, scale: 0.92f);
                        break;
                    case CellContent.Snake:
                        DrawSnake(px, py);
                        break;
                    case CellContent.Gem:
                        DrawGem(px, py);
                        break;
                    case CellContent.HealthPickup:
                        DrawHealthPickup(px, py);
                        break;
                    case CellContent.Shield:
                        DrawShield(px, py);
                        break;
                    case CellContent.SpeedBoost:
                        DrawSpeedBoost(px, py);
                        break;
					case CellContent.Empty:
						break;
				}

				if (_session.Rat.Position == pos)
					DrawRat(px, py, ratTargeted);
			}
		}

		DrawEffects(gridOriginX, gridOriginY);
	}

    private void DrawCrosshair(int px, int py, float pulse)
    {
        if (_sprites is null)
            return;

        var scale = 0.70f + (0.18f * pulse);
        var tint = new Color(255, 255, 255, (int)(190 + 65 * pulse));
        DrawSprite(_sprites.Crosshair, px, py, scale: scale, tint: tint);
    }

	private void DrawRat(int px, int py, bool targeted)
	{
		if (_sprites is null)
			return;

		var frame = ((int)(_elapsedSeconds * 6) % 2) == 0 ? _sprites.RatA : _sprites.RatB;
		var flash = _ratHitFlashSecondsLeft <= 0 ? 0f : Math.Clamp(_ratHitFlashSecondsLeft / 0.22f, 0f, 1f);
		var tint = targeted || flash > 0
			? new Color(255, (int)(120 + 90 * (1 - flash)), (int)(120 + 90 * (1 - flash)), 255)
			: Color.White;
		DrawSprite(frame, px, py, scale: 0.96f, tint: tint);

        if (targeted)
            Raylib.DrawRectangleLines(px + 2, py + 2, TileSize - 4, TileSize - 4, Shot);
            
        // Draw shield effect around rat
        if (_session.Rat.HasShield)
        {
            var shieldPulse = 0.6f + 0.4f * MathF.Sin((float)_elapsedSeconds * 4f);
            var shieldAlpha = (int)(150 * shieldPulse);
            var shieldColor = new Color(70, 130, 200, shieldAlpha);
            Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * 0.55f, shieldColor);
            Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * 0.50f, shieldColor);
        }
        
        // Draw speed boost effect (trail particles)
        if (_session.Rat.HasSpeedBoost)
        {
            var speedPulse = MathF.Abs(MathF.Sin((float)_elapsedSeconds * 8f));
            var speedColor = new Color(255, 230, 80, (int)(100 * speedPulse));
            Raylib.DrawCircle(px + TileSize / 2 - 8, py + TileSize / 2, 3, speedColor);
            Raylib.DrawCircle(px + TileSize / 2 + 8, py + TileSize / 2, 3, speedColor);
        }
    }

    private void DrawSnake(int px, int py)
    {
        if (_sprites is null)
            return;

        var frame = ((int)(_elapsedSeconds * 5) % 2) == 0 ? _sprites.SnakeA : _sprites.SnakeB;
        DrawSprite(frame, px, py, scale: 0.94f);
    }

    private void DrawGem(int px, int py)
    {
        if (_sprites is null)
            return;

        var frame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
        DrawSprite(frame, px, py, scale: 0.94f);
    }
    
    private void DrawHealthPickup(int px, int py)
    {
        if (_sprites is null)
            return;

        var pulse = 0.90f + 0.10f * MathF.Sin((float)_elapsedSeconds * 3f);
        DrawSprite(_sprites.HealthPickup, px, py, scale: pulse);
    }
    
    private void DrawShield(int px, int py)
    {
        if (_sprites is null)
            return;

        var pulse = 0.88f + 0.12f * MathF.Sin((float)_elapsedSeconds * 2.5f);
        var glow = (int)(180 + 75 * MathF.Sin((float)_elapsedSeconds * 4f));
        var tint = new Color(255, 255, 255, glow);
        DrawSprite(_sprites.Shield, px, py, scale: pulse, tint: tint);
    }
    
    private void DrawSpeedBoost(int px, int py)
    {
        if (_sprites is null)
            return;

        var pulse = 0.85f + 0.15f * MathF.Sin((float)_elapsedSeconds * 5f);
        var glow = (int)(200 + 55 * MathF.Sin((float)_elapsedSeconds * 6f));
        var tint = new Color(255, 255, glow, 255);
        DrawSprite(_sprites.SpeedBoost, px, py, scale: pulse, tint: tint);
    }

    private void DrawHearts(int x, int y)
    {
        if (_sprites is null)
            return;

        var spacing = 20;
        var size = 18;

        for (var i = 0; i < _session.Rat.MaxHealth; i++)
        {
            var tex = i < _session.Rat.Health ? _sprites.HeartFull : _sprites.HeartEmpty;
            DrawSpriteAt(tex, x + (i * spacing), y, size, Color.White);
        }
    }

    private void DrawSprite(Texture2D texture, int px, int py, float scale, Color? tint = null)
    {
        var target = tint ?? Color.White;

        var size = TileSize * scale;
        var src = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(px + TileSize / 2f, py + TileSize / 2f, size, size);
        var origin = new Vector2(size / 2f, size / 2f);

        Raylib.DrawTexturePro(texture, src, dest, origin, rotation: 0, target);
    }

    private static void DrawSpriteAt(Texture2D texture, int x, int y, int size, Color tint)
    {
        var src = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(x + size / 2f, y + size / 2f, size, size);
        var origin = new Vector2(size / 2f, size / 2f);

        Raylib.DrawTexturePro(texture, src, dest, origin, rotation: 0, tint);
    }

	private void DrawMessages()
	{
		if (_messageLog.Count == 0)
			return;

		var x = Margin * 2 + _session.Level.Width * TileSize + (int)_shakeOffset.X;
		var startY = Margin + 160 + (int)_shakeOffset.Y;
		var panelBottom = Margin + _session.Level.Height * TileSize + (int)_shakeOffset.Y;
		var hintReserve = 72;

        var available = panelBottom - hintReserve - startY;
        var maxLines = Math.Max(0, available / 18);
        if (maxLines <= 0)
            return;

        var messagesToShow = _messageLog.TakeLast(Math.Min(maxLines, _messageLog.Count)).ToArray();

        Raylib.DrawText("Events", x, startY - 28, 18, HudText);

        var y = startY;
        foreach (var message in messagesToShow)
        {
            var color = message.Kind switch
            {
                GameMessageKind.Success => new Color(120, 220, 120, 255),
                GameMessageKind.Damage => new Color(255, 120, 120, 255),
                GameMessageKind.Warning => new Color(255, 220, 130, 255),
                GameMessageKind.Bonus => new Color(255, 230, 80, 255),
                GameMessageKind.PowerUp => new Color(100, 180, 255, 255),
                _ => HudText,
            };

            Raylib.DrawText($"• {message.Text}", x, y, 16, color);
            y += 18;
        }
    }

	private void DrawOverlayHints()
	{
		var x = Margin * 2 + _session.Level.Width * TileSize + (int)_shakeOffset.X;
		var y = Margin + _session.Level.Height * TileSize - 80 + (int)_shakeOffset.Y;
        var text = _session.Status switch
        {
            SessionStatus.InProgress => "Move: WASD/Arrows\nQuit: Q  (Shots land when timer hits 0)",
            SessionStatus.ChapterComplete => $"Chapter complete!\nScore: {_session.Rat.Score}\nNext: N / Enter",
            SessionStatus.GameOver => $"Game over!\nFinal Score: {_session.Rat.Score}\nMax Combo: {_session.Rat.MaxCombo} | Gems: {_session.Rat.GemsCollected}\nRestart: R",
            _ => string.Empty,
        };

		if (!string.IsNullOrWhiteSpace(text))
		{
			var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < lines.Length; i++)
				Raylib.DrawText(lines[i], x, y + (i * 18), 16, HudText);
		}
	}

	private void HandleEvents(IReadOnlyList<GameEvent> events)
	{
		for (var i = 0; i < events.Count; i++)
		{
			var gameEvent = events[i];
			switch (gameEvent.Kind)
			{
				case GameEventKind.ShotWaveFired:
					if (gameEvent.Positions is not null)
					{
						foreach (var target in gameEvent.Positions)
							_effects.Add(new VisualEffect(VisualEffectKind.ShotImpact, target, seconds: 0.18f));
					}
					break;
				case GameEventKind.ShotHit:
					_ratHitFlashSecondsLeft = 0.22f;
					StartShake(seconds: 0.18, strength: 6);
					break;
				case GameEventKind.SnakeBite:
					if (gameEvent.Position is { } bitePos)
						_effects.Add(new VisualEffect(VisualEffectKind.SnakeBite, bitePos, seconds: 0.25f));
					_ratHitFlashSecondsLeft = 0.28f;
					StartShake(seconds: 0.24, strength: 7);
					break;
				case GameEventKind.GemFound:
					if (gameEvent.Position is { } gemPos)
						_effects.Add(new VisualEffect(VisualEffectKind.GemSparkle, gemPos, seconds: 0.60f));
					break;
				case GameEventKind.HealthPickup:
					if (gameEvent.Position is { } healthPos)
						_effects.Add(new VisualEffect(VisualEffectKind.HealthPickup, healthPos, seconds: 0.40f));
					break;
				case GameEventKind.ShieldPickup:
					if (gameEvent.Position is { } shieldPos)
						_effects.Add(new VisualEffect(VisualEffectKind.ShieldPickup, shieldPos, seconds: 0.50f));
					break;
				case GameEventKind.SpeedBoostPickup:
					if (gameEvent.Position is { } speedPos)
						_effects.Add(new VisualEffect(VisualEffectKind.SpeedBoostPickup, speedPos, seconds: 0.40f));
					break;
				case GameEventKind.RockDug:
					if (gameEvent.Position is { } rockPos)
						_effects.Add(new VisualEffect(VisualEffectKind.RockDug, rockPos, seconds: 0.35f));
					StartShake(seconds: 0.12, strength: 4);
					break;
				case GameEventKind.ComboMilestone:
					_effects.Add(new VisualEffect(VisualEffectKind.ComboFlash, _session.Rat.Position, seconds: 0.50f));
					break;
			}
		}
	}

	private void UpdateEffects(float dt)
	{
		if (_ratHitFlashSecondsLeft > 0)
			_ratHitFlashSecondsLeft = Math.Max(0, _ratHitFlashSecondsLeft - dt);

		for (var i = _effects.Count - 1; i >= 0; i--)
		{
			var effect = _effects[i];
			effect.TimeLeftSeconds -= dt;
			if (effect.TimeLeftSeconds <= 0)
				_effects.RemoveAt(i);
		}
	}

	private void DrawEffects(int gridOriginX, int gridOriginY)
	{
		for (var i = 0; i < _effects.Count; i++)
		{
			var effect = _effects[i];
			var pos = effect.Position;

			var px = gridOriginX + pos.X * TileSize;
			var py = gridOriginY + pos.Y * TileSize;

			var t = effect.TotalSeconds <= 0.0001f ? 0f : Math.Clamp(effect.TimeLeftSeconds / effect.TotalSeconds, 0f, 1f);
			switch (effect.Kind)
			{
				case VisualEffectKind.ShotImpact:
				{
					var alpha = (int)(255 * t);
					var fill = new Color(225, 70, 70, alpha);
					Raylib.DrawRectangle(px + 2, py + 2, TileSize - 4, TileSize - 4, fill);
					break;
				}
				case VisualEffectKind.SnakeBite:
				{
					var alpha = (int)(220 * t);
					var color = new Color(66, 196, 98, alpha);
					Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, (TileSize * 0.42f), color);
					break;
				}
				case VisualEffectKind.GemSparkle:
				{
					var alpha = (int)(240 * t);
					var color = new Color(255, 255, 255, alpha);
					var cx = px + TileSize / 2;
					var cy = py + TileSize / 2;
					var r = (int)(TileSize * (0.18f + 0.16f * (1 - t)));
					Raylib.DrawLine(cx - r, cy, cx + r, cy, color);
					Raylib.DrawLine(cx, cy - r, cx, cy + r, color);
					break;
				}
				case VisualEffectKind.HealthPickup:
				{
					var alpha = (int)(200 * t);
					var color = new Color(240, 80, 80, alpha);
					var radius = TileSize * (0.3f + 0.3f * (1 - t));
					Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, radius, color);
					break;
				}
				case VisualEffectKind.ShieldPickup:
				{
					var alpha = (int)(220 * t);
					var color = new Color(70, 130, 200, alpha);
					var radius = TileSize * (0.25f + 0.35f * (1 - t));
					Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, radius, color);
					Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, radius * 0.7f, color);
					break;
				}
				case VisualEffectKind.SpeedBoostPickup:
				{
					var alpha = (int)(200 * t);
					var color = new Color(255, 230, 80, alpha);
					var cx = px + TileSize / 2;
					var cy = py + TileSize / 2;
					var r = (int)(TileSize * (0.2f + 0.2f * (1 - t)));
					// Lightning bolt shape
					Raylib.DrawLine(cx - r, cy - r, cx, cy, color);
					Raylib.DrawLine(cx, cy, cx + r, cy + r, color);
					break;
				}
				case VisualEffectKind.RockDug:
				{
					var alpha = (int)(180 * t);
					var color = new Color(170, 170, 186, alpha);
					var spread = (1 - t) * TileSize * 0.5f;
					Raylib.DrawCircle((int)(px + TileSize / 2 - spread), (int)(py + TileSize / 2 - spread), 3, color);
					Raylib.DrawCircle((int)(px + TileSize / 2 + spread), (int)(py + TileSize / 2 - spread), 3, color);
					Raylib.DrawCircle((int)(px + TileSize / 2), (int)(py + TileSize / 2 + spread), 3, color);
					break;
				}
				case VisualEffectKind.ComboFlash:
				{
					var alpha = (int)(150 * t);
					var color = new Color(255, 230, 100, alpha);
					var radius = TileSize * (0.5f + 0.5f * (1 - t));
					Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, radius, color);
					break;
				}
			}
		}
	}

	private void StartShake(double seconds, float strength)
	{
		_shakeSecondsLeft = Math.Max(_shakeSecondsLeft, seconds);
		_shakeStrength = Math.Max(_shakeStrength, strength);
	}

	private void UpdateShake(float dt)
	{
		if (_shakeSecondsLeft <= 0)
		{
			_shakeOffset = Vector2.Zero;
			_shakeStrength = 6f;
			return;
		}

		_shakeSecondsLeft = Math.Max(0, _shakeSecondsLeft - dt);

		var fade = (float)Math.Clamp(_shakeSecondsLeft / 0.30, 0.0, 1.0);
		var amount = _shakeStrength * fade;

		var sx = MathF.Sin((float)_elapsedSeconds * 45f) * amount;
		var sy = MathF.Cos((float)_elapsedSeconds * 52f) * amount;
		_shakeOffset = new Vector2(sx, sy);
	}

	private sealed class VisualEffect
	{
		public VisualEffect(VisualEffectKind kind, Position position, float seconds)
		{
			Kind = kind;
			Position = position;
			TotalSeconds = seconds;
			TimeLeftSeconds = seconds;
		}

		public VisualEffectKind Kind { get; }
		public Position Position { get; }
		public float TotalSeconds { get; }
		public float TimeLeftSeconds { get; set; }
	}

	private enum VisualEffectKind
	{
		ShotImpact,
		SnakeBite,
		GemSparkle,
		HealthPickup,
		ShieldPickup,
		SpeedBoostPickup,
		RockDug,
		ComboFlash,
	}
}
