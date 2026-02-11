using Rat.Game;
using Raylib_cs;
using System.Numerics;

namespace Rat.Desktop;

internal sealed class RatDesktopApp
{
    private const int TileSize = 40;
    private const int Margin = 20;
    private const int SidePanelWidth = 320;
    private const int ButtonWidth = 200;
    private const int ButtonHeight = 50;

    private readonly int? _seed;
    private RealTimeGameSession _session;
    private readonly List<GameMessage> _messageLog = new();
    private SpriteTextures? _sprites;
    private readonly List<VisualEffect> _effects = new();
    private readonly List<ParticleEffect> _particles = new();

    private double _elapsedSeconds;
    private bool _exitRequested;
    private double _shakeSecondsLeft;
    private float _shakeStrength = 6f;
    private float _ratHitFlashSecondsLeft;
    private Vector2 _shakeOffset;
    
    // Overlay animation
    private float _overlayFadeIn;
    private float _overlayScale;
    private const float OverlayFadeSpeed = 3f;
    
    // Delay before showing popup (to show the gem first)
    private float _popupDelaySeconds;
    private const float ChapterCompleteDelay = 1.5f;
    private const float GameOverDelay = 1.0f;
    private bool _popupDelayStarted;

    private static readonly Color Background = new(22, 24, 30, 255);
    private static readonly Color GridBackground = new(35, 38, 48, 255);
    private static readonly Color HudText = new(235, 235, 240, 255);
    private static readonly Color HudDim = new(160, 160, 170, 255);
    private static readonly Color Dust = new(95, 85, 70, 255);
    private static readonly Color Dug = new(45, 48, 58, 255);
    private static readonly Color Border = new(28, 30, 38, 255);
    private static readonly Color Shot = new(255, 80, 80, 255);
    private static readonly Color Success = new(80, 220, 120, 255);
    private static readonly Color Gold = new(255, 220, 80, 255);
    private static readonly Color ButtonColor = new(70, 130, 200, 255);
    private static readonly Color ButtonHover = new(90, 150, 220, 255);

    public RatDesktopApp(int? seed)
    {
        _seed = seed;
        _session = new RealTimeGameSession(seed: seed);
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1100, 800, "RAT - Find the Gem!");
        Raylib.SetTargetFPS(60);
        _sprites = new SpriteTextures();

        ResizeWindowToLevel();

        try
        {
            while (!Raylib.WindowShouldClose() && !_exitRequested)
            {
                var dt = Raylib.GetFrameTime();
                _elapsedSeconds += dt;

                Direction? moveDirection = null;
                
                if (_session.Status == SessionStatus.InProgress)
                {
                    moveDirection = ReadMoveDirection();
                }

                var outcome = _session.Update(dt, moveDirection);
                AppendMessages(outcome.Messages);
                HandleEvents(outcome.Events);
                UpdateEffects(dt);
                UpdateParticles(dt);
                UpdateShake(dt);
                UpdateOverlay(dt);

                HandleInput();

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Background);
                
                DrawGrid();
                DrawHud();
                DrawMessages();
                
                if (_session.Status == SessionStatus.ChapterComplete)
                    DrawChapterCompleteOverlay();
                else if (_session.Status == SessionStatus.GameOver)
                    DrawGameOverOverlay();
                else
                    DrawInGameHints();
                
                Raylib.EndDrawing();
            }
        }
        finally
        {
            _sprites?.Dispose();
            _sprites = null;
            Raylib.CloseWindow();
        }
    }

    private void HandleInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Q) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            _exitRequested = true;
            return;
        }

        if (_session.Status == SessionStatus.InProgress)
            return;

        var mousePos = Raylib.GetMousePosition();
        var clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        if (_session.Status == SessionStatus.ChapterComplete)
        {
            // Only allow input after the popup has appeared
            if (_popupDelaySeconds > 0)
                return;
                
            var buttonRect = GetCenterButtonRect(0);
            if (clicked && CheckCollisionPointRec(mousePos, buttonRect) ||
                Raylib.IsKeyPressed(KeyboardKey.Enter) || 
                Raylib.IsKeyPressed(KeyboardKey.Space) ||
                Raylib.IsKeyPressed(KeyboardKey.N))
            {
                _session.StartNextChapter();
                _messageLog.Clear();
                _effects.Clear();
                _particles.Clear();
                _overlayFadeIn = 0;
                _overlayScale = 0;
                _popupDelayStarted = false;
                _popupDelaySeconds = 0;
                ResizeWindowToLevel();
            }
        }

        if (_session.Status == SessionStatus.GameOver)
        {
            // Only allow input after the popup has appeared
            if (_popupDelaySeconds > 0)
                return;
                
            var buttonRect = GetCenterButtonRect(0);
            if (clicked && CheckCollisionPointRec(mousePos, buttonRect) ||
                Raylib.IsKeyPressed(KeyboardKey.Enter) || 
                Raylib.IsKeyPressed(KeyboardKey.Space) ||
                Raylib.IsKeyPressed(KeyboardKey.R))
            {
                _session = new RealTimeGameSession(seed: _seed);
                _messageLog.Clear();
                _effects.Clear();
                _particles.Clear();
                _overlayFadeIn = 0;
                _overlayScale = 0;
                _popupDelayStarted = false;
                _popupDelaySeconds = 0;
                ResizeWindowToLevel();
            }
            
            var quitRect = GetCenterButtonRect(1);
            if (clicked && CheckCollisionPointRec(mousePos, quitRect))
            {
                _exitRequested = true;
            }
        }
    }

    private void UpdateOverlay(float dt)
    {
        if (_session.Status != SessionStatus.InProgress)
        {
            // Start the popup delay if not already started
            if (!_popupDelayStarted)
            {
                _popupDelayStarted = true;
                _popupDelaySeconds = _session.Status == SessionStatus.ChapterComplete 
                    ? ChapterCompleteDelay 
                    : GameOverDelay;
            }
            
            // Count down the delay
            if (_popupDelaySeconds > 0)
            {
                _popupDelaySeconds -= dt;
                return; // Don't fade in the overlay yet
            }
            
            // Now fade in the overlay
            _overlayFadeIn = Math.Min(1f, _overlayFadeIn + dt * OverlayFadeSpeed);
            _overlayScale = EaseOutBack(_overlayFadeIn);
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
    }

    private Rectangle GetCenterButtonRect(int buttonIndex)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;
        var centerY = screenH / 2 + 60 + buttonIndex * (ButtonHeight + 15);
        return new Rectangle(centerX - ButtonWidth / 2, centerY, ButtonWidth, ButtonHeight);
    }

    private static bool CheckCollisionPointRec(Vector2 point, Rectangle rec)
    {
        return point.X >= rec.X && point.X <= rec.X + rec.Width &&
               point.Y >= rec.Y && point.Y <= rec.Y + rec.Height;
    }

    private void ResizeWindowToLevel()
    {
        var gridWidth = _session.Level.Width * TileSize;
        var gridHeight = _session.Level.Height * TileSize;
        var width = Margin * 3 + gridWidth + SidePanelWidth;
        var height = Margin * 2 + Math.Max(gridHeight, 500);
        Raylib.SetWindowSize(Math.Max(900, width), Math.Max(600, height));
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

        const int maxMessages = 8;
        while (_messageLog.Count > maxMessages)
            _messageLog.RemoveAt(0);
    }

    private void DrawHud()
    {
        if (_sprites is null)
            return;

        var gridWidth = _session.Level.Width * TileSize;
        var panelX = Margin * 2 + gridWidth + (int)_shakeOffset.X;
        var x = panelX + 10;
        var y = Margin + (int)_shakeOffset.Y;

        var panelHeight = Math.Max(_session.Level.Height * TileSize, 480);
        Raylib.DrawRectangle(panelX, Margin, SidePanelWidth, panelHeight, new Color(30, 32, 40, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(panelX, Margin, SidePanelWidth, panelHeight), 2, Border);

        Raylib.DrawText("RAT", x, y, 36, Gold);
        Raylib.DrawText("Find the Gem!", x + 80, y + 10, 18, HudDim);

        y += 50;
        
        Raylib.DrawText($"Chapter {_session.ChapterNumber}", x, y, 22, HudText);
        y += 28;
        
        Raylib.DrawText("Score:", x, y, 18, HudDim);
        Raylib.DrawText($"{_session.Rat.Score}", x + 70, y, 22, Gold);

        if (_session.Rat.ComboCount > 0)
        {
            var comboColor = _session.Rat.ComboCount >= 10 
                ? Gold 
                : new Color(200, 200, 210, 255);
            Raylib.DrawText($"Combo: x{_session.Rat.ComboCount}", x + 160, y, 18, comboColor);
        }
        
        y += 32;
        
        DrawHearts(x, y);
        y += 30;

        if (_session.Rat.HasShield)
        {
            var shieldColor = new Color(100, 180, 255, 255);
            Raylib.DrawText($"SHIELD: {_session.Rat.ShieldSecondsRemaining:0.0}s", x, y, 16, shieldColor);
            y += 20;
        }
        if (_session.Rat.HasSpeedBoost)
        {
            var speedColor = Gold;
            Raylib.DrawText($"SPEED: {_session.Rat.SpeedBoostSecondsRemaining:0.0}s", x, y, 16, speedColor);
            y += 20;
        }
        if (_session.Rat.RockDigsRemaining > 0)
        {
            Raylib.DrawText($"Rock Digs: {_session.Rat.RockDigsRemaining}", x, y, 16, HudDim);
            y += 20;
        }

        y += 10;

        Raylib.DrawRectangle(x - 5, y, SidePanelWidth - 20, 70, new Color(40, 25, 30, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(x - 5, y, SidePanelWidth - 20, 70), 1, Shot);
        
        y += 8;
        Raylib.DrawText($"Incoming Shots: {_session.ChapterSettings.ShotsPerTurn}", x, y, 16, Shot);
        y += 22;

        var time = Math.Max(0, _session.TimeUntilShotsFireSeconds);
        var urgencyColor = time < 0.5 ? Shot : HudText;
        Raylib.DrawText($"Impact in: {time:0.0}s", x, y, 20, urgencyColor);
        y += 26;

        var barX = x;
        var barW = SidePanelWidth - 30;
        var barH = 12;
        Raylib.DrawRectangle(barX, y, barW, barH, Border);

        var t = _session.ShotTelegraphSeconds <= 0.0001 ? 0 : (float)(time / _session.ShotTelegraphSeconds);
        var fill = (int)(barW * Math.Clamp(t, 0f, 1f));
        
        var barColor = time < 0.5 
            ? new Color(255, 60, 60, 255)
            : new Color(255, 150, 80, 255);
        Raylib.DrawRectangle(barX, y, fill, barH, barColor);
    }

    private void DrawGrid()
    {
        if (_sprites is null)
            return;

        var gridOriginX = Margin + (int)_shakeOffset.X;
        var gridOriginY = Margin + (int)_shakeOffset.Y;

        var level = _session.Level;
        var gridWidth = level.Width * TileSize;
        var gridHeight = level.Height * TileSize;

        Raylib.DrawRectangle(gridOriginX - 4, gridOriginY - 4, gridWidth + 8, gridHeight + 8, GridBackground);
        Raylib.DrawRectangleLinesEx(new Rectangle(gridOriginX - 4, gridOriginY - 4, gridWidth + 8, gridHeight + 8), 3, Border);

        var revealAll = _session.Status is SessionStatus.ChapterComplete or SessionStatus.GameOver;
        var shotTargets = _session.TelegraphedShots.Select(s => s.Target).ToHashSet();
        var ratTargeted = shotTargets.Contains(_session.Rat.Position);

        var timeRatio = _session.ShotTelegraphSeconds <= 0.0001 ? 0f : (float)(_session.TimeUntilShotsFireSeconds / _session.ShotTelegraphSeconds);
        var urgency = 1f - Math.Clamp(timeRatio, 0f, 1f);
        var pulseSpeed = 3f + urgency * 8f;
        var pulse = 0.4f + 0.6f * MathF.Abs(MathF.Sin((float)_elapsedSeconds * pulseSpeed));

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
                    : revealAll ? new Color(75, 68, 55, 255) : Dust;
                Raylib.DrawRectangle(px, py, TileSize, TileSize, baseColor);
                
                Raylib.DrawRectangleLines(px, py, TileSize, TileSize, new Color(40, 42, 50, 255));

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
                        DrawSprite(_sprites.Rock, px, py, scale: 1.0f);
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

        DrawParticles(gridOriginX, gridOriginY);
        DrawEffects(gridOriginX, gridOriginY);

        if (revealAll || level.GetCell(level.GemPosition).IsDug)
        {
            DrawGemIndicator(gridOriginX, gridOriginY);
        }
    }

    private void DrawGemIndicator(int gridOriginX, int gridOriginY)
    {
        var gemPos = _session.Level.GemPosition;
        var px = gridOriginX + gemPos.X * TileSize;
        var py = gridOriginY + gemPos.Y * TileSize;
        
        var glowPulse = 0.3f + 0.3f * MathF.Sin((float)_elapsedSeconds * 2f);
        var glowAlpha = (int)(60 * glowPulse);
        var glowColor = new Color(100, 220, 255, glowAlpha);
        
        for (var i = 3; i >= 1; i--)
        {
            Raylib.DrawRectangleLinesEx(
                new Rectangle(px - i * 2, py - i * 2, TileSize + i * 4, TileSize + i * 4),
                2, new Color(glowColor.R, glowColor.G, glowColor.B, (byte)(glowAlpha / i)));
        }
    }

    private void DrawCrosshair(int px, int py, float pulse)
    {
        if (_sprites is null)
            return;

        var scale = 0.85f + (0.15f * pulse);
        var alpha = (int)(180 + 75 * pulse);
        var tint = new Color(255, 255, 255, alpha);
        DrawSprite(_sprites.Crosshair, px, py, scale: scale, tint: tint);
        
        var glowAlpha = (int)(40 * pulse);
        Raylib.DrawRectangle(px + 2, py + 2, TileSize - 4, TileSize - 4, new Color(255, 50, 50, glowAlpha));
    }

    private void DrawRat(int px, int py, bool targeted)
    {
        if (_sprites is null)
            return;

        var frame = ((int)(_elapsedSeconds * 6) % 2) == 0 ? _sprites.RatA : _sprites.RatB;
        var flash = _ratHitFlashSecondsLeft <= 0 ? 0f : Math.Clamp(_ratHitFlashSecondsLeft / 0.22f, 0f, 1f);
        var tint = targeted || flash > 0
            ? new Color(255, (int)(140 + 80 * (1 - flash)), (int)(140 + 80 * (1 - flash)), 255)
            : Color.White;
        DrawSprite(frame, px, py, scale: 1.1f, tint: tint);

        if (targeted)
        {
            var dangerPulse = MathF.Abs(MathF.Sin((float)_elapsedSeconds * 8f));
            var dangerAlpha = (int)(150 + 100 * dangerPulse);
            Raylib.DrawRectangleLinesEx(new Rectangle(px + 2, py + 2, TileSize - 4, TileSize - 4), 2, 
                new Color(255, 50, 50, dangerAlpha));
        }

        if (_session.Rat.HasShield)
        {
            var shieldPulse = 0.6f + 0.4f * MathF.Sin((float)_elapsedSeconds * 4f);
            var shieldAlpha = (int)(180 * shieldPulse);
            var shieldColor = new Color(100, 180, 255, shieldAlpha);
            Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * 0.6f, shieldColor);
            Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * 0.55f, shieldColor);
        }

        if (_session.Rat.HasSpeedBoost)
        {
            var speedPulse = MathF.Abs(MathF.Sin((float)_elapsedSeconds * 10f));
            var speedColor = new Color(255, 230, 80, (int)(120 * speedPulse));
            Raylib.DrawCircle(px + TileSize / 2 - 10, py + TileSize / 2, 4, speedColor);
            Raylib.DrawCircle(px + TileSize / 2 + 10, py + TileSize / 2, 4, speedColor);
        }
    }

    private void DrawSnake(int px, int py)
    {
        if (_sprites is null)
            return;

        var frame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.SnakeA : _sprites.SnakeB;
        DrawSprite(frame, px, py, scale: 1.0f);
    }

    private void DrawGem(int px, int py)
    {
        if (_sprites is null)
            return;

        var frame = ((int)(_elapsedSeconds * 3) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
        var pulse = 1.0f + 0.1f * MathF.Sin((float)_elapsedSeconds * 4f);
        DrawSprite(frame, px, py, scale: pulse);
        
        var glowPulse = 0.4f + 0.4f * MathF.Sin((float)_elapsedSeconds * 2f);
        var glowAlpha = (int)(80 * glowPulse);
        Raylib.DrawCircle(px + TileSize / 2, py + TileSize / 2, TileSize * 0.6f, new Color(100, 220, 255, glowAlpha));
    }

    private void DrawHealthPickup(int px, int py)
    {
        if (_sprites is null)
            return;

        var pulse = 0.95f + 0.10f * MathF.Sin((float)_elapsedSeconds * 3f);
        DrawSprite(_sprites.HealthPickup, px, py, scale: pulse);
    }

    private void DrawShield(int px, int py)
    {
        if (_sprites is null)
            return;

        var pulse = 0.95f + 0.10f * MathF.Sin((float)_elapsedSeconds * 2.5f);
        var glow = (int)(200 + 55 * MathF.Sin((float)_elapsedSeconds * 4f));
        var tint = new Color(255, 255, 255, glow);
        DrawSprite(_sprites.Shield, px, py, scale: pulse, tint: tint);
    }

    private void DrawSpeedBoost(int px, int py)
    {
        if (_sprites is null)
            return;

        var pulse = 0.92f + 0.12f * MathF.Sin((float)_elapsedSeconds * 5f);
        var glow = (int)(220 + 35 * MathF.Sin((float)_elapsedSeconds * 6f));
        var tint = new Color(255, 255, glow, 255);
        DrawSprite(_sprites.SpeedBoost, px, py, scale: pulse, tint: tint);
    }

    private void DrawHearts(int x, int y)
    {
        if (_sprites is null)
            return;

        var spacing = 26;
        var size = 22;

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

        var gridWidth = _session.Level.Width * TileSize;
        var x = Margin * 2 + gridWidth + (int)_shakeOffset.X + 10;
        var startY = Margin + 250 + (int)_shakeOffset.Y;
        
        Raylib.DrawText("--- Events ---", x, startY - 25, 14, HudDim);

        var y = startY;
        var messagesToShow = _messageLog.TakeLast(6).ToArray();

        foreach (var message in messagesToShow)
        {
            var color = message.Kind switch
            {
                GameMessageKind.Success => Success,
                GameMessageKind.Damage => Shot,
                GameMessageKind.Warning => new Color(255, 200, 100, 255),
                GameMessageKind.Bonus => Gold,
                GameMessageKind.PowerUp => new Color(100, 200, 255, 255),
                _ => HudText,
            };

            Raylib.DrawText($"* {message.Text}", x, y, 14, color);
            y += 18;
        }
    }

    private void DrawInGameHints()
    {
        var gridWidth = _session.Level.Width * TileSize;
        var x = Margin * 2 + gridWidth + (int)_shakeOffset.X + 10;
        var gridHeight = _session.Level.Height * TileSize;
        var y = Margin + Math.Max(gridHeight, 400) - 100;

        Raylib.DrawRectangle(x - 5, y, SidePanelWidth - 20, 90, new Color(35, 38, 48, 255));
        
        var lines = new[]
        {
            "Controls:",
            "WASD / Arrow Keys - Move",
            "ESC / Q - Quit",
            "",
            "Find the gem to complete the chapter!"
        };

        for (var i = 0; i < lines.Length; i++)
        {
            var lineColor = i == 0 ? HudText : HudDim;
            Raylib.DrawText(lines[i], x, y + 5 + i * 16, 14, lineColor);
        }
    }

    private void DrawChapterCompleteOverlay()
    {
        if (_sprites is null)
            return;

        // Don't draw overlay during the delay period (let player see the gem)
        if (_popupDelaySeconds > 0)
            return;

        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        var bgAlpha = (int)(230 * _overlayFadeIn);
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(20, 40, 30, bgAlpha));

        var scale = _overlayScale;
        var centerX = screenW / 2;
        var centerY = screenH / 2 - 50;

        var panelW = (int)(400 * scale);
        var panelH = (int)(320 * scale);
        Raylib.DrawRectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH, new Color(30, 50, 40, 240));
        Raylib.DrawRectangleLinesEx(new Rectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH), 3, Success);

        if (scale > 0.5f)
        {
            DrawSpriteAt(_sprites.Trophy, centerX - 24, centerY - panelH / 2 + 20, 48, Color.White);

            var title = "CHAPTER COMPLETE!";
            var titleW = Raylib.MeasureText(title, 32);
            Raylib.DrawText(title, centerX - titleW / 2, centerY - panelH / 2 + 80, 32, Success);

            var statsY = centerY - panelH / 2 + 130;
            var statLines = new[]
            {
                $"Chapter {_session.ChapterNumber} cleared!",
                $"Score: {_session.Rat.Score}",
                $"Health: {_session.Rat.Health}/{_session.Rat.MaxHealth}",
                $"Combo: x{_session.Rat.ComboCount}"
            };

            foreach (var line in statLines)
            {
                var lineW = Raylib.MeasureText(line, 20);
                Raylib.DrawText(line, centerX - lineW / 2, statsY, 20, HudText);
                statsY += 28;
            }

            DrawButton(GetCenterButtonRect(0), "NEXT CHAPTER", Success);
        }

        if (_overlayFadeIn > 0.3f && _particles.Count < 30)
        {
            SpawnCelebrationParticles();
        }
    }

    private void DrawGameOverOverlay()
    {
        if (_sprites is null)
            return;

        // Don't draw overlay during the delay period
        if (_popupDelaySeconds > 0)
            return;

        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        var bgAlpha = (int)(230 * _overlayFadeIn);
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(40, 20, 20, bgAlpha));

        var scale = _overlayScale;
        var centerX = screenW / 2;
        var centerY = screenH / 2 - 50;

        var panelW = (int)(400 * scale);
        var panelH = (int)(380 * scale);
        Raylib.DrawRectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH, new Color(50, 30, 30, 240));
        Raylib.DrawRectangleLinesEx(new Rectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH), 3, Shot);

        if (scale > 0.5f)
        {
            DrawSpriteAt(_sprites.Skull, centerX - 24, centerY - panelH / 2 + 20, 48, Color.White);

            var title = "GAME OVER";
            var titleW = Raylib.MeasureText(title, 36);
            Raylib.DrawText(title, centerX - titleW / 2, centerY - panelH / 2 + 80, 36, Shot);

            var statsY = centerY - panelH / 2 + 135;
            var statLines = new[]
            {
                $"Final Score: {_session.Rat.Score}",
                $"Chapters Cleared: {_session.ChapterNumber - 1}",
                $"Gems Collected: {_session.Rat.GemsCollected}",
                $"Max Combo: x{_session.Rat.MaxCombo}",
                $"Shots Dodged: {_session.Rat.ShotsAvoided}"
            };

            foreach (var line in statLines)
            {
                var lineW = Raylib.MeasureText(line, 18);
                Raylib.DrawText(line, centerX - lineW / 2, statsY, 18, HudText);
                statsY += 26;
            }

            DrawButton(GetCenterButtonRect(0), "PLAY AGAIN", ButtonColor);
            DrawButton(GetCenterButtonRect(1), "QUIT", new Color(100, 100, 110, 255));
        }
    }

    private void DrawButton(Rectangle rect, string text, Color color)
    {
        var mousePos = Raylib.GetMousePosition();
        var isHovered = CheckCollisionPointRec(mousePos, rect);
        
        var buttonColor = isHovered ? ButtonHover : color;
        var borderColor = isHovered ? Color.White : new Color(200, 200, 210, 255);

        Raylib.DrawRectangleRec(rect, buttonColor);
        Raylib.DrawRectangleLinesEx(rect, 2, borderColor);

        var textW = Raylib.MeasureText(text, 22);
        var textX = (int)(rect.X + rect.Width / 2 - textW / 2);
        var textY = (int)(rect.Y + rect.Height / 2 - 11);
        Raylib.DrawText(text, textX, textY, 22, Color.White);
    }

    private void SpawnCelebrationParticles()
    {
        var screenW = Raylib.GetScreenWidth();
        var random = new Random();
        
        for (var i = 0; i < 3; i++)
        {
            _particles.Add(new ParticleEffect
            {
                X = random.Next(0, screenW),
                Y = -20,
                VelocityX = (float)(random.NextDouble() - 0.5) * 50,
                VelocityY = (float)(random.NextDouble() * 100 + 50),
                Color = (i % 3) switch
                {
                    0 => Gold,
                    1 => Success,
                    _ => new Color(100, 200, 255, 255)
                },
                Size = random.Next(4, 10),
                LifeSeconds = 3f
            });
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
                            _effects.Add(new VisualEffect(VisualEffectKind.ShotImpact, target, seconds: 0.25f));
                    }
                    break;
                case GameEventKind.ShotHit:
                    _ratHitFlashSecondsLeft = 0.25f;
                    StartShake(seconds: 0.20, strength: 8);
                    break;
                case GameEventKind.SnakeBite:
                    if (gameEvent.Position is { } bitePos)
                        _effects.Add(new VisualEffect(VisualEffectKind.SnakeBite, bitePos, seconds: 0.30f));
                    _ratHitFlashSecondsLeft = 0.30f;
                    StartShake(seconds: 0.25, strength: 8);
                    break;
                case GameEventKind.GemFound:
                    if (gameEvent.Position is { } gemPos)
                        _effects.Add(new VisualEffect(VisualEffectKind.GemSparkle, gemPos, seconds: 0.80f));
                    break;
                case GameEventKind.HealthPickup:
                    if (gameEvent.Position is { } healthPos)
                        _effects.Add(new VisualEffect(VisualEffectKind.HealthPickup, healthPos, seconds: 0.45f));
                    break;
                case GameEventKind.ShieldPickup:
                    if (gameEvent.Position is { } shieldPos)
                        _effects.Add(new VisualEffect(VisualEffectKind.ShieldPickup, shieldPos, seconds: 0.55f));
                    break;
                case GameEventKind.SpeedBoostPickup:
                    if (gameEvent.Position is { } speedPos)
                        _effects.Add(new VisualEffect(VisualEffectKind.SpeedBoostPickup, speedPos, seconds: 0.45f));
                    break;
                case GameEventKind.RockDug:
                    if (gameEvent.Position is { } rockPos)
                        _effects.Add(new VisualEffect(VisualEffectKind.RockDug, rockPos, seconds: 0.40f));
                    StartShake(seconds: 0.15, strength: 5);
                    break;
                case GameEventKind.ComboMilestone:
                    _effects.Add(new VisualEffect(VisualEffectKind.ComboFlash, _session.Rat.Position, seconds: 0.55f));
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

    private void UpdateParticles(float dt)
    {
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.X += p.VelocityX * dt;
            p.Y += p.VelocityY * dt;
            p.VelocityY += 100 * dt;
            p.LifeSeconds -= dt;
            
            if (p.LifeSeconds <= 0 || p.Y > Raylib.GetScreenHeight() + 50)
                _particles.RemoveAt(i);
        }
    }

    private void DrawParticles(int gridOriginX, int gridOriginY)
    {
        foreach (var p in _particles)
        {
            var alpha = (int)(255 * Math.Min(1, p.LifeSeconds));
            var color = new Color(p.Color.R, p.Color.G, p.Color.B, (byte)alpha);
            Raylib.DrawCircle((int)p.X, (int)p.Y, p.Size, color);
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
                    var alpha = (int)(200 * t);
                    var fill = new Color(255, 80, 80, alpha);
                    Raylib.DrawRectangle(px + 3, py + 3, TileSize - 6, TileSize - 6, fill);
                    break;
                }
                case VisualEffectKind.SnakeBite:
                {
                    var alpha = (int)(220 * t);
                    var color = new Color(80, 200, 100, alpha);
                    var radius = TileSize * (0.3f + 0.2f * (1 - t));
                    Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, radius, color);
                    break;
                }
                case VisualEffectKind.GemSparkle:
                {
                    var alpha = (int)(255 * t);
                    var color = new Color(255, 255, 255, alpha);
                    var cx = px + TileSize / 2;
                    var cy = py + TileSize / 2;
                    var r = (int)(TileSize * (0.2f + 0.3f * (1 - t)));
                    Raylib.DrawLine(cx - r, cy, cx + r, cy, color);
                    Raylib.DrawLine(cx, cy - r, cx, cy + r, color);
                    var d = (int)(r * 0.7f);
                    Raylib.DrawLine(cx - d, cy - d, cx + d, cy + d, color);
                    Raylib.DrawLine(cx + d, cy - d, cx - d, cy + d, color);
                    break;
                }
                case VisualEffectKind.HealthPickup:
                {
                    var alpha = (int)(200 * t);
                    var color = new Color(240, 80, 80, alpha);
                    var radius = TileSize * (0.3f + 0.35f * (1 - t));
                    Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, radius, color);
                    break;
                }
                case VisualEffectKind.ShieldPickup:
                {
                    var alpha = (int)(220 * t);
                    var color = new Color(100, 180, 255, alpha);
                    var radius = TileSize * (0.25f + 0.4f * (1 - t));
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
                    var r = (int)(TileSize * (0.25f + 0.25f * (1 - t)));
                    Raylib.DrawLine(cx - r, cy - r, cx, cy, color);
                    Raylib.DrawLine(cx, cy, cx + r, cy + r, color);
                    break;
                }
                case VisualEffectKind.RockDug:
                {
                    var alpha = (int)(180 * t);
                    var color = new Color(170, 170, 186, alpha);
                    var spread = (1 - t) * TileSize * 0.6f;
                    Raylib.DrawCircle((int)(px + TileSize / 2 - spread), (int)(py + TileSize / 2 - spread), 4, color);
                    Raylib.DrawCircle((int)(px + TileSize / 2 + spread), (int)(py + TileSize / 2 - spread), 4, color);
                    Raylib.DrawCircle((int)(px + TileSize / 2), (int)(py + TileSize / 2 + spread), 4, color);
                    Raylib.DrawCircle((int)(px + TileSize / 2 - spread * 0.5f), (int)(py + TileSize / 2 + spread * 0.5f), 3, color);
                    break;
                }
                case VisualEffectKind.ComboFlash:
                {
                    var alpha = (int)(150 * t);
                    var color = new Color(255, 230, 100, alpha);
                    var radius = TileSize * (0.5f + 0.6f * (1 - t));
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

        var sx = MathF.Sin((float)_elapsedSeconds * 50f) * amount;
        var sy = MathF.Cos((float)_elapsedSeconds * 55f) * amount;
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

    private sealed class ParticleEffect
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; }
        public float LifeSeconds { get; set; }
    }
}
