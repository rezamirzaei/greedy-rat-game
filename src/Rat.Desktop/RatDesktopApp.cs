using Rat.Game;
using Raylib_cs;
using System.Numerics;

namespace Rat.Desktop;

internal sealed class RatDesktopApp
{
    private const int TileSize = 40;
    private const int Margin = 20;
    private const int SidePanelWidth = 320;
    private const int ButtonWidth = 220;
    private const int ButtonHeight = 50;

    private readonly int? _seed;
    private RealTimeGameSession _session = null!;
    private readonly List<GameMessage> _messageLog = new();
    private SpriteTextures? _sprites;
    private readonly List<VisualEffect> _effects = new();
    private readonly List<ParticleEffect> _particles = new();
    private readonly List<FloatingText> _floatingTexts = new();
    private readonly List<ConsumedItem> _consumedItems = new();

    private double _elapsedSeconds;
    private bool _exitRequested;
    private double _shakeSecondsLeft;
    private float _shakeStrength = 6f;
    private float _ratHitFlashSecondsLeft;
    private Vector2 _shakeOffset;
    
    private GameState _gameState = GameState.MainMenu;
    private float _menuAnimationTime;
    
    private float _overlayFadeIn;
    private float _overlayScale;
    private const float OverlayFadeSpeed = 3.5f;
    
    private float _popupDelaySeconds;
    private const float ChapterCompleteDelay = 1.8f;
    private const float GameOverDelay = 1.2f;
    private bool _popupDelayStarted;
    
    private float _levelTransitionAlpha;
    private bool _isTransitioning;

    private static readonly Color Background = new(18, 20, 28, 255);
    private static readonly Color GridBackground = new(32, 35, 45, 255);
    private static readonly Color HudText = new(240, 240, 245, 255);
    private static readonly Color HudDim = new(140, 145, 160, 255);
    private static readonly Color Dust = new(85, 75, 65, 255);
    private static readonly Color DustPattern = new(75, 68, 58, 255);
    private static readonly Color Dug = new(42, 45, 55, 255);
    private static readonly Color Border = new(25, 28, 35, 255);
    private static readonly Color Shot = new(255, 70, 70, 255);
    private static readonly Color Success = new(70, 220, 110, 255);
    private static readonly Color Gold = new(255, 215, 70, 255);
    private static readonly Color Cyan = new(70, 200, 255, 255);
    private static readonly Color ButtonColor = new(60, 120, 200, 255);
    private static readonly Color ButtonHover = new(80, 145, 230, 255);
    private static readonly Color ButtonSuccess = new(50, 180, 100, 255);
    private static readonly Color ButtonSuccessHover = new(70, 210, 120, 255);

    public RatDesktopApp(int? seed)
    {
        _seed = seed;
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(1000, 700, "RAT - Find the Gem!");
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);
        _sprites = new SpriteTextures();

        try
        {
            while (!Raylib.WindowShouldClose() && !_exitRequested)
            {
                var dt = Raylib.GetFrameTime();
                _elapsedSeconds += dt;
                _menuAnimationTime += dt;

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Background);

                switch (_gameState)
                {
                    case GameState.MainMenu:
                        UpdateMainMenu();
                        DrawMainMenu();
                        break;
                    case GameState.Playing:
                        UpdateGame(dt);
                        DrawGame();
                        break;
                }

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

    private void StartNewGame()
    {
        _session = new RealTimeGameSession(seed: _seed);
        _messageLog.Clear();
        _effects.Clear();
        _particles.Clear();
        _floatingTexts.Clear();
        _consumedItems.Clear();
        _overlayFadeIn = 0;
        _overlayScale = 0;
        _popupDelayStarted = false;
        _popupDelaySeconds = 0;
        _gameState = GameState.Playing;
        _isTransitioning = true;
        _levelTransitionAlpha = 1f;
        ResizeWindowToLevel();
    }

    private void UpdateMainMenu()
    {
        var mousePos = Raylib.GetMousePosition();
        var clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        var playRect = GetMenuButtonRect(0);
        if ((clicked && CheckCollisionPointRec(mousePos, playRect)) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter) ||
            Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            StartNewGame();
        }

        var quitRect = GetMenuButtonRect(1);
        if (clicked && CheckCollisionPointRec(mousePos, quitRect))
        {
            _exitRequested = true;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Q))
        {
            _exitRequested = true;
        }
    }

    private void DrawMainMenu()
    {
        if (_sprites is null) return;

        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;

        DrawMenuBackground();

        var titleY = screenH / 4;
        var titleBounce = MathF.Sin((float)_menuAnimationTime * 2f) * 8f;
        
        var title = "RAT";
        var titleW = Raylib.MeasureText(title, 80);
        Raylib.DrawText(title, centerX - titleW / 2 + 4, (int)(titleY + titleBounce) + 4, 80, new Color(0, 0, 0, 100));
        Raylib.DrawText(title, centerX - titleW / 2, (int)(titleY + titleBounce), 80, Gold);

        var subtitle = "Find the Gem!";
        var subtitleW = Raylib.MeasureText(subtitle, 28);
        Raylib.DrawText(subtitle, centerX - subtitleW / 2, titleY + 90, 28, Cyan);

        var ratY = titleY + 140;
        var ratBob = MathF.Sin((float)_menuAnimationTime * 4f) * 3f;
        var ratFrame = ((int)(_menuAnimationTime * 6) % 2) == 0 ? _sprites.RatA : _sprites.RatB;
        DrawSpriteAtCenter(ratFrame, centerX, (int)(ratY + ratBob), 64);

        var gemX = centerX + 50;
        var gemPulse = 1f + 0.1f * MathF.Sin((float)_menuAnimationTime * 3f);
        var gemFrame = ((int)(_menuAnimationTime * 3) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
        DrawSpriteAtCenter(gemFrame, gemX, (int)(ratY + ratBob), (int)(48 * gemPulse));

        DrawMenuButton(GetMenuButtonRect(0), "PLAY", ButtonSuccess, ButtonSuccessHover);
        DrawMenuButton(GetMenuButtonRect(1), "QUIT", new Color(100, 100, 115, 255), new Color(130, 130, 145, 255));

        var instructY = screenH - 100;
        var instr1 = "Use WASD or Arrow Keys to move";
        var instr2 = "Avoid shots and snakes, find the gem!";
        var instr1W = Raylib.MeasureText(instr1, 16);
        var instr2W = Raylib.MeasureText(instr2, 16);
        Raylib.DrawText(instr1, centerX - instr1W / 2, instructY, 16, HudDim);
        Raylib.DrawText(instr2, centerX - instr2W / 2, instructY + 22, 16, HudDim);

        Raylib.DrawText("v1.0", 10, screenH - 25, 14, new Color(80, 80, 90, 255));
    }

    private void DrawMenuBackground()
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var gridSize = 50;
        var offset = (int)(_menuAnimationTime * 20) % gridSize;

        for (var x = -gridSize + offset; x < screenW + gridSize; x += gridSize)
        {
            for (var y = -gridSize + offset; y < screenH + gridSize; y += gridSize)
            {
                var alpha = (int)(20 + 10 * MathF.Sin((x + y) * 0.02f + (float)_menuAnimationTime));
                Raylib.DrawRectangleLines(x, y, gridSize - 2, gridSize - 2, new Color(50, 55, 70, alpha));
            }
        }

        for (var i = 0; i < 20; i++)
        {
            var px = (int)((MathF.Sin((float)_menuAnimationTime * 0.5f + i * 0.7f) + 1) * 0.5f * screenW);
            var py = (int)((MathF.Cos((float)_menuAnimationTime * 0.3f + i * 1.1f) + 1) * 0.5f * screenH);
            var size = 2 + (i % 3);
            var alpha = 30 + (i % 4) * 15;
            var color = i % 3 == 0 ? Gold : (i % 3 == 1 ? Cyan : Success);
            Raylib.DrawCircle(px, py, size, new Color(color.R, color.G, color.B, (byte)alpha));
        }
    }

    private Rectangle GetMenuButtonRect(int index)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;
        var buttonY = screenH / 2 + 80 + index * (ButtonHeight + 20);
        return new Rectangle(centerX - ButtonWidth / 2, buttonY, ButtonWidth, ButtonHeight);
    }

    private void DrawMenuButton(Rectangle rect, string text, Color normalColor, Color hoverColor)
    {
        var mousePos = Raylib.GetMousePosition();
        var isHovered = CheckCollisionPointRec(mousePos, rect);
        var color = isHovered ? hoverColor : normalColor;
        var scale = isHovered ? 1.02f : 1f;

        var scaledRect = new Rectangle(
            rect.X - (rect.Width * (scale - 1) / 2),
            rect.Y - (rect.Height * (scale - 1) / 2),
            rect.Width * scale,
            rect.Height * scale
        );

        Raylib.DrawRectangle((int)scaledRect.X + 3, (int)scaledRect.Y + 3, (int)scaledRect.Width, (int)scaledRect.Height, new Color(0, 0, 0, 60));
        Raylib.DrawRectangleRec(scaledRect, color);
        Raylib.DrawRectangleLinesEx(scaledRect, 2, isHovered ? Color.White : new Color(255, 255, 255, 150));

        var textW = Raylib.MeasureText(text, 24);
        var textX = (int)(scaledRect.X + scaledRect.Width / 2 - textW / 2);
        var textY = (int)(scaledRect.Y + scaledRect.Height / 2 - 12);
        Raylib.DrawText(text, textX, textY, 24, Color.White);
    }

    private void UpdateGame(float dt)
    {
        if (_isTransitioning)
        {
            _levelTransitionAlpha = Math.Max(0, _levelTransitionAlpha - dt * 3f);
            if (_levelTransitionAlpha <= 0) _isTransitioning = false;
        }

        Direction? moveDirection = null;
        if (_session.Status == SessionStatus.InProgress)
            moveDirection = ReadMoveDirection();

        var outcome = _session.Update(dt, moveDirection);
        AppendMessages(outcome.Messages);
        HandleEvents(outcome.Events);
        UpdateEffects(dt);
        UpdateParticles(dt);
        UpdateFloatingTexts(dt);
        UpdateConsumedItems(dt);
        UpdateShake(dt);
        UpdateOverlay(dt);
        HandleGameInput();
    }

    private void DrawGame()
    {
        DrawGrid();
        DrawHud();
        DrawMessages();

        if (_session.Status == SessionStatus.ChapterComplete)
            DrawChapterCompleteOverlay();
        else if (_session.Status == SessionStatus.GameOver)
            DrawGameOverOverlay();
        else
            DrawInGameHints();

        DrawFloatingTexts();

        if (_isTransitioning && _levelTransitionAlpha > 0)
        {
            var alpha = (int)(255 * _levelTransitionAlpha);
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(18, 20, 28, alpha));
        }
    }

    private void HandleGameInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            if (_session.Status == SessionStatus.InProgress)
            {
                _gameState = GameState.MainMenu;
                return;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Q))
        {
            _gameState = GameState.MainMenu;
            return;
        }

        if (_session.Status == SessionStatus.InProgress) return;

        var mousePos = Raylib.GetMousePosition();
        var clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        if (_session.Status == SessionStatus.ChapterComplete)
        {
            if (_popupDelaySeconds > 0) return;

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
                _floatingTexts.Clear();
                _consumedItems.Clear();
                _overlayFadeIn = 0;
                _overlayScale = 0;
                _popupDelayStarted = false;
                _popupDelaySeconds = 0;
                _isTransitioning = true;
                _levelTransitionAlpha = 1f;
                ResizeWindowToLevel();
            }
        }

        if (_session.Status == SessionStatus.GameOver)
        {
            if (_popupDelaySeconds > 0) return;

            var buttonRect = GetCenterButtonRect(0);
            if (clicked && CheckCollisionPointRec(mousePos, buttonRect) ||
                Raylib.IsKeyPressed(KeyboardKey.Enter) ||
                Raylib.IsKeyPressed(KeyboardKey.Space) ||
                Raylib.IsKeyPressed(KeyboardKey.R))
            {
                StartNewGame();
            }

            var menuRect = GetCenterButtonRect(1);
            if (clicked && CheckCollisionPointRec(mousePos, menuRect))
            {
                _gameState = GameState.MainMenu;
            }
        }
    }

    private void UpdateOverlay(float dt)
    {
        if (_session.Status != SessionStatus.InProgress)
        {
            if (!_popupDelayStarted)
            {
                _popupDelayStarted = true;
                _popupDelaySeconds = _session.Status == SessionStatus.ChapterComplete ? ChapterCompleteDelay : GameOverDelay;
            }

            if (_popupDelaySeconds > 0)
            {
                _popupDelaySeconds -= dt;
                return;
            }

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
        var centerY = screenH / 2 + 70 + buttonIndex * (ButtonHeight + 15);
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
        Raylib.SetWindowSize(Math.Max(950, width), Math.Max(650, height));
    }

    private static Direction? ReadMoveDirection()
    {
        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) return Direction.Up;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) return Direction.Down;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) return Direction.Left;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) return Direction.Right;
        return null;
    }

    private void AppendMessages(IReadOnlyList<GameMessage> messages)
    {
        if (messages.Count == 0) return;
        foreach (var message in messages) _messageLog.Add(message);
        const int maxMessages = 8;
        while (_messageLog.Count > maxMessages) _messageLog.RemoveAt(0);
    }

    private void DrawHud()
    {
        if (_sprites is null) return;

        var gridWidth = _session.Level.Width * TileSize;
        var panelX = Margin * 2 + gridWidth + (int)_shakeOffset.X;
        var x = panelX + 12;
        var y = Margin + (int)_shakeOffset.Y;

        var panelHeight = Math.Max(_session.Level.Height * TileSize, 480);
        
        Raylib.DrawRectangle(panelX, Margin, SidePanelWidth, panelHeight, new Color(28, 30, 38, 255));
        Raylib.DrawRectangle(panelX, Margin, SidePanelWidth, 60, new Color(35, 38, 48, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(panelX, Margin, SidePanelWidth, panelHeight), 2, Border);

        Raylib.DrawText("RAT", x, y + 8, 40, Gold);
        
        var chapterText = $"Chapter {_session.ChapterNumber}";
        var chapterW = Raylib.MeasureText(chapterText, 18);
        Raylib.DrawRectangle(x + 100, y + 12, chapterW + 16, 26, new Color(60, 65, 80, 255));
        Raylib.DrawText(chapterText, x + 108, y + 16, 18, HudText);

        y += 70;

        Raylib.DrawText("SCORE", x, y, 14, HudDim);
        y += 18;
        Raylib.DrawText($"{_session.Rat.Score:N0}", x, y, 32, Gold);

        if (_session.Rat.ComboCount > 0)
        {
            var comboX = x + 140;
            var comboPulse = _session.Rat.ComboCount >= 10 ? 1f + 0.1f * MathF.Sin((float)_elapsedSeconds * 8f) : 1f;
            var comboColor = _session.Rat.ComboCount >= 10 ? Gold : _session.Rat.ComboCount >= 5 ? Cyan : new Color(180, 180, 190, 255);
            Raylib.DrawText("COMBO", comboX, y - 18, 12, HudDim);
            Raylib.DrawText($"x{_session.Rat.ComboCount}", comboX, y, (int)(24 * comboPulse), comboColor);
        }

        y += 45;
        Raylib.DrawText("HEALTH", x, y, 14, HudDim);
        y += 20;
        DrawHearts(x, y);
        y += 35;

        if (_session.Rat.HasShield || _session.Rat.HasSpeedBoost || _session.Rat.RockDigsRemaining > 0)
        {
            Raylib.DrawText("POWER-UPS", x, y, 14, HudDim);
            y += 20;

            if (_session.Rat.HasShield)
            {
                var shieldPulse = 0.8f + 0.2f * MathF.Sin((float)_elapsedSeconds * 4f);
                Raylib.DrawRectangle(x - 2, y - 2, SidePanelWidth - 24, 22, new Color(40, 80, 120, 100));
                Raylib.DrawText($"SHIELD  {_session.Rat.ShieldSecondsRemaining:0.0}s", x, y, 16, new Color(100, 180, 255, (int)(255 * shieldPulse)));
                y += 24;
            }
            if (_session.Rat.HasSpeedBoost)
            {
                var speedPulse = 0.8f + 0.2f * MathF.Sin((float)_elapsedSeconds * 6f);
                Raylib.DrawRectangle(x - 2, y - 2, SidePanelWidth - 24, 22, new Color(120, 100, 40, 100));
                Raylib.DrawText($"SPEED  {_session.Rat.SpeedBoostSecondsRemaining:0.0}s", x, y, 16, new Color(255, 230, 80, (int)(255 * speedPulse)));
                y += 24;
            }
            if (_session.Rat.RockDigsRemaining > 0)
            {
                Raylib.DrawText($"ROCK DIGS: {_session.Rat.RockDigsRemaining}", x, y, 16, HudDim);
                y += 24;
            }
            y += 5;
        }

        y += 10;
        var timeRatio = _session.ShotTelegraphSeconds <= 0.0001 ? 0f : (float)(_session.TimeUntilShotsFireSeconds / _session.ShotTelegraphSeconds);
        var dangerLevel = 1f - Math.Clamp(timeRatio, 0f, 1f);
        var dangerPulse = dangerLevel > 0.7f ? (0.8f + 0.2f * MathF.Sin((float)_elapsedSeconds * 10f)) : 1f;
        
        Raylib.DrawRectangle(x - 5, y, SidePanelWidth - 24, 85, new Color((byte)(40 + (int)(40 * dangerLevel)), (byte)25, (byte)30, (byte)255));
        Raylib.DrawRectangleLinesEx(new Rectangle(x - 5, y, SidePanelWidth - 24, 85), 2, new Color(Shot.R, Shot.G, Shot.B, (byte)(150 + (int)(105 * dangerPulse))));

        y += 10;
        Raylib.DrawText("INCOMING FIRE", x, y, 14, Shot);
        y += 20;

        var time = Math.Max(0, _session.TimeUntilShotsFireSeconds);
        var timeColor = time < 0.5 ? new Color((byte)255, (byte)(100 + (int)(155 * dangerPulse)), (byte)(100 + (int)(155 * dangerPulse)), (byte)255) : HudText;
        Raylib.DrawText($"{_session.ChapterSettings.ShotsPerTurn} shots in {time:0.0}s", x, y, 20, timeColor);
        y += 28;

        var barW = SidePanelWidth - 34;
        Raylib.DrawRectangle(x, y, barW, 14, new Color(20, 22, 28, 255));
        var fill = (int)(barW * Math.Clamp(timeRatio, 0f, 1f));
        var barColor = time < 0.5 ? new Color((byte)255, (byte)(60 + (int)(60 * dangerPulse)), (byte)(60 + (int)(60 * dangerPulse)),(byte) 255) : new Color(255, 140, 60, 255);
        Raylib.DrawRectangle(x, y, fill, 14, barColor);
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y, barW, 14), 1, Border);
    }

    private void DrawGrid()
    {
        if (_sprites is null) return;

        var gridOriginX = Margin + (int)_shakeOffset.X;
        var gridOriginY = Margin + (int)_shakeOffset.Y;
        var level = _session.Level;
        var gridWidth = level.Width * TileSize;
        var gridHeight = level.Height * TileSize;

        Raylib.DrawRectangle(gridOriginX - 6, gridOriginY - 6, gridWidth + 12, gridHeight + 12, new Color(15, 17, 22, 255));
        Raylib.DrawRectangle(gridOriginX - 3, gridOriginY - 3, gridWidth + 6, gridHeight + 6, GridBackground);

        var revealAll = _session.Status is SessionStatus.ChapterComplete or SessionStatus.GameOver;
        var shotTargets = _session.TelegraphedShots.Select(s => s.Target).ToHashSet();
        var ratTargeted = shotTargets.Contains(_session.Rat.Position);

        var timeRatio = _session.ShotTelegraphSeconds <= 0.0001 ? 0f : (float)(_session.TimeUntilShotsFireSeconds / _session.ShotTelegraphSeconds);
        var urgency = 1f - Math.Clamp(timeRatio, 0f, 1f);
        var pulse = 0.4f + 0.6f * MathF.Abs(MathF.Sin((float)_elapsedSeconds * (3f + urgency * 10f)));

        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var pos = new Position(x, y);
                var cell = level.GetCell(pos);
                var px = gridOriginX + x * TileSize;
                var py = gridOriginY + y * TileSize;

                var baseColor = cell.IsDug ? Dug : (revealAll ? new Color(65, 58, 50, 255) : Dust);
                Raylib.DrawRectangle(px, py, TileSize, TileSize, baseColor);

                if (!cell.IsDug && !revealAll && (x + y) % 2 == 0)
                    Raylib.DrawRectangle(px + 2, py + 2, TileSize - 4, TileSize - 4, DustPattern);

                Raylib.DrawRectangleLines(px, py, TileSize, TileSize, new Color(35, 38, 45, 255));

                if (shotTargets.Contains(pos))
                    DrawCrosshair(px, py, pulse, urgency);

                if (!cell.IsDug && !revealAll)
                {
                    if (_session.Rat.Position == pos) DrawRat(px, py, ratTargeted);
                    continue;
                }

                switch (cell.Content)
                {
                    case CellContent.Rock: DrawSprite(_sprites.Rock, px, py, 1.0f); break;
                    case CellContent.Snake: DrawSnake(px, py); break;
                    case CellContent.Gem: DrawGem(px, py); break;
                    case CellContent.HealthPickup: DrawHealthPickup(px, py); break;
                    case CellContent.Shield: DrawShield(px, py); break;
                    case CellContent.SpeedBoost: DrawSpeedBoost(px, py); break;
                }

                if (_session.Rat.Position == pos) DrawRat(px, py, ratTargeted);
            }
        }

        DrawParticles();
        DrawEffects(gridOriginX, gridOriginY);
        DrawConsumedItems(gridOriginX, gridOriginY);

        if (revealAll || level.GetCell(level.GemPosition).IsDug)
            DrawGemIndicator(gridOriginX, gridOriginY);
    }

    private void DrawConsumedItems(int gridOriginX, int gridOriginY)
    {
        if (_sprites is null) return;
        
        foreach (var item in _consumedItems)
        {
            var px = gridOriginX + item.Position.X * TileSize;
            var py = gridOriginY + item.Position.Y * TileSize;
            var t = item.TotalSeconds <= 0.0001f ? 0f : Math.Clamp(item.TimeLeftSeconds / item.TotalSeconds, 0f, 1f);
            var alpha = (int)(255 * t);
            var tint = new Color(255, 255, 255, alpha);
            var scale = 0.9f + 0.2f * t;
            
            switch (item.Content)
            {
                case CellContent.Snake:
                    var snakeFrame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.SnakeA : _sprites.SnakeB;
                    DrawSprite(snakeFrame, px, py, scale, tint);
                    break;
                case CellContent.Gem:
                    var gemFrame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
                    DrawSprite(gemFrame, px, py, scale, tint);
                    break;
                case CellContent.HealthPickup:
                    DrawSprite(_sprites.HealthPickup, px, py, scale, tint);
                    break;
                case CellContent.Shield:
                    DrawSprite(_sprites.Shield, px, py, scale, tint);
                    break;
                case CellContent.SpeedBoost:
                    DrawSprite(_sprites.SpeedBoost, px, py, scale, tint);
                    break;
            }
        }
    }

    private void DrawGemIndicator(int gridOriginX, int gridOriginY)
    {
        var gemPos = _session.Level.GemPosition;
        var px = gridOriginX + gemPos.X * TileSize;
        var py = gridOriginY + gemPos.Y * TileSize;
        var glowPulse = 0.5f + 0.5f * MathF.Sin((float)_elapsedSeconds * 3f);
        for (var i = 4; i >= 1; i--)
            Raylib.DrawRectangleLinesEx(new Rectangle(px - i * 3, py - i * 3, TileSize + i * 6, TileSize + i * 6), 2, new Color(100, 220, 255, (int)(40 * glowPulse / i)));
    }

    private void DrawCrosshair(int px, int py, float pulse, float urgency)
    {
        if (_sprites is null) return;
        DrawSprite(_sprites.Crosshair, px, py, 0.85f + 0.20f * pulse, new Color(255, 255, 255, (int)(160 + 95 * pulse)));
        Raylib.DrawRectangle(px + 2, py + 2, TileSize - 4, TileSize - 4, new Color(255, 40, 40, (int)(30 + 50 * urgency * pulse)));
    }

    private void DrawRat(int px, int py, bool targeted)
    {
        if (_sprites is null) return;
        var frame = ((int)(_elapsedSeconds * 6) % 2) == 0 ? _sprites.RatA : _sprites.RatB;
        var flash = _ratHitFlashSecondsLeft <= 0 ? 0f : Math.Clamp(_ratHitFlashSecondsLeft / 0.22f, 0f, 1f);
        var tint = targeted || flash > 0 ? new Color(255, (int)(130 + 90 * (1 - flash)), (int)(130 + 90 * (1 - flash)), 255) : Color.White;
        var bob = MathF.Sin((float)_elapsedSeconds * 5f) * 1.5f;
        DrawSprite(frame, px, (int)(py - bob), 1.15f, tint);

        if (targeted)
            Raylib.DrawRectangleLinesEx(new Rectangle(px + 1, py + 1, TileSize - 2, TileSize - 2), 3, new Color(255, 50, 50, (int)(120 + 135 * MathF.Abs(MathF.Sin((float)_elapsedSeconds * 10f)))));

        if (_session.Rat.HasShield)
            Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * 0.65f, new Color(80, 160, 255, (int)(200 * (0.6f + 0.4f * MathF.Sin((float)_elapsedSeconds * 5f)))));

        if (_session.Rat.HasSpeedBoost)
        {
            var speedPulse = MathF.Abs(MathF.Sin((float)_elapsedSeconds * 12f));
            Raylib.DrawCircle(px + TileSize / 2 - 12, py + TileSize / 2, 5, new Color(255, 230, 80, (int)(150 * speedPulse)));
            Raylib.DrawCircle(px + TileSize / 2 + 12, py + TileSize / 2, 5, new Color(255, 230, 80, (int)(150 * speedPulse)));
        }
    }

    private void DrawSnake(int px, int py)
    {
        if (_sprites is null) return;
        var frame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.SnakeA : _sprites.SnakeB;
        DrawSprite(frame, (int)(px + MathF.Sin((float)_elapsedSeconds * 3f + px * 0.1f) * 1.5f), py, 1.0f);
    }

    private void DrawGem(int px, int py)
    {
        if (_sprites is null) return;
        var frame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
        var bob = MathF.Sin((float)_elapsedSeconds * 3f) * 2f;
        DrawSprite(frame, px, (int)(py - bob), 1.0f + 0.12f * MathF.Sin((float)_elapsedSeconds * 5f));
        Raylib.DrawCircle(px + TileSize / 2, py + TileSize / 2, TileSize * 0.55f, new Color(100, 220, 255, (int)(100 * (0.5f + 0.5f * MathF.Sin((float)_elapsedSeconds * 3f)))));
    }

    private void DrawHealthPickup(int px, int py)
    {
        if (_sprites is null) return;
        DrawSprite(_sprites.HealthPickup, px, (int)(py - MathF.Sin((float)_elapsedSeconds * 2.5f) * 2f), 0.95f + 0.10f * MathF.Sin((float)_elapsedSeconds * 4f));
    }

    private void DrawShield(int px, int py)
    {
        if (_sprites is null) return;
        DrawSprite(_sprites.Shield, px, (int)(py - MathF.Sin((float)_elapsedSeconds * 2f) * 2f), 0.95f + 0.10f * MathF.Sin((float)_elapsedSeconds * 3f), new Color(255, 255, 255, (int)(200 + 55 * MathF.Sin((float)_elapsedSeconds * 4f))));
    }

    private void DrawSpeedBoost(int px, int py)
    {
        if (_sprites is null) return;
        DrawSprite(_sprites.SpeedBoost, px, (int)(py - MathF.Sin((float)_elapsedSeconds * 3f) * 2f), 0.92f + 0.15f * MathF.Sin((float)_elapsedSeconds * 6f));
    }

    private void DrawHearts(int x, int y)
    {
        if (_sprites is null) return;
        for (var i = 0; i < _session.Rat.MaxHealth; i++)
        {
            var tex = i < _session.Rat.Health ? _sprites.HeartFull : _sprites.HeartEmpty;
            var heartPulse = i < _session.Rat.Health ? (1f + 0.05f * MathF.Sin((float)_elapsedSeconds * 3f + i * 0.5f)) : 1f;
            DrawSpriteAt(tex, x + i * 28, y, (int)(24 * heartPulse), Color.White);
        }
    }

    private void DrawSprite(Texture2D texture, int px, int py, float scale, Color? tint = null)
    {
        var size = TileSize * scale;
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Rectangle(px + TileSize / 2f, py + TileSize / 2f, size, size), new Vector2(size / 2f, size / 2f), 0, tint ?? Color.White);
    }

    private static void DrawSpriteAt(Texture2D texture, int x, int y, int size, Color tint)
    {
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Rectangle(x + size / 2f, y + size / 2f, size, size), new Vector2(size / 2f, size / 2f), 0, tint);
    }

    private void DrawSpriteAtCenter(Texture2D texture, int cx, int cy, int size)
    {
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), new Rectangle(cx, cy, size, size), new Vector2(size / 2f, size / 2f), 0, Color.White);
    }

    private void DrawMessages()
    {
        if (_messageLog.Count == 0) return;
        var gridWidth = _session.Level.Width * TileSize;
        var x = Margin * 2 + gridWidth + (int)_shakeOffset.X + 12;
        var startY = Margin + 280 + (int)_shakeOffset.Y;

        Raylib.DrawText("EVENTS", x, startY - 22, 14, HudDim);
        Raylib.DrawLine(x, startY - 5, x + SidePanelWidth - 40, startY - 5, new Color(50, 55, 65, 255));

        var y = startY;
        foreach (var (message, i) in _messageLog.TakeLast(6).Select((m, i) => (m, i)))
        {
            var alpha = 150 + i * 17;
            var color = message.Kind switch
            {
                GameMessageKind.Success => new Color(Success.R, Success.G, Success.B, (byte)alpha),
                GameMessageKind.Damage => new Color(Shot.R, Shot.G, Shot.B, (byte)alpha),
                GameMessageKind.Warning => new Color(255, 200, 100, alpha),
                GameMessageKind.Bonus => new Color(Gold.R, Gold.G, Gold.B, (byte)alpha),
                GameMessageKind.PowerUp => new Color(Cyan.R, Cyan.G, Cyan.B, (byte)alpha),
                _ => new Color(HudText.R, HudText.G, HudText.B, (byte)alpha),
            };
            Raylib.DrawText($"• {message.Text}", x, y, 14, color);
            y += 18;
        }
    }

    private void DrawInGameHints()
    {
        var gridWidth = _session.Level.Width * TileSize;
        var x = Margin * 2 + gridWidth + (int)_shakeOffset.X + 12;
        var y = Margin + Math.Max(_session.Level.Height * TileSize, 400) - 90;

        Raylib.DrawRectangle(x - 5, y, SidePanelWidth - 24, 80, new Color(32, 35, 42, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(x - 5, y, SidePanelWidth - 24, 80), 1, new Color(45, 48, 58, 255));

        Raylib.DrawText("CONTROLS", x, y + 8, 13, HudDim);
        Raylib.DrawText("WASD / Arrows - Move", x, y + 23, 14, HudDim);
        Raylib.DrawText("ESC - Menu  |  Q - Quit", x, y + 38, 14, HudDim);
        Raylib.DrawText("Find the gem to win!", x, y + 58, 14, Cyan);
    }

    private void DrawChapterCompleteOverlay()
    {
        if (_sprites is null || _popupDelaySeconds > 0) return;

        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(15, 35, 25, (int)(235 * _overlayFadeIn)));

        var scale = _overlayScale;
        var centerX = screenW / 2;
        var centerY = screenH / 2 - 40;
        var panelW = (int)(420 * scale);
        var panelH = (int)(340 * scale);

        Raylib.DrawRectangle(centerX - panelW / 2 + 5, centerY - panelH / 2 + 5, panelW, panelH, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH, new Color(25, 45, 35, 250));
        Raylib.DrawRectangleLinesEx(new Rectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH), 3, Success);

        if (scale > 0.5f)
        {
            DrawSpriteAtCenter(_sprites.Trophy, centerX, centerY - panelH / 2 + 45, 56);
            var title = "CHAPTER COMPLETE!";
            Raylib.DrawText(title, centerX - Raylib.MeasureText(title, 32) / 2, centerY - panelH / 2 + 85, 32, Success);

            var statsY = centerY - panelH / 2 + 135;
            DrawStatLine(centerX, statsY, "Chapter", $"{_session.ChapterNumber}", Gold); statsY += 30;
            DrawStatLine(centerX, statsY, "Score", $"{_session.Rat.Score:N0}", Gold); statsY += 30;
            DrawStatLine(centerX, statsY, "Health", $"{_session.Rat.Health}/{_session.Rat.MaxHealth}", _session.Rat.Health == _session.Rat.MaxHealth ? Success : HudText); statsY += 30;
            DrawStatLine(centerX, statsY, "Combo", $"x{_session.Rat.ComboCount}", _session.Rat.ComboCount >= 10 ? Gold : HudText);

            DrawButton(GetCenterButtonRect(0), "NEXT CHAPTER", ButtonSuccess, ButtonSuccessHover);
        }

        if (_overlayFadeIn > 0.3f && _particles.Count < 40) SpawnCelebrationParticles();
    }

    private void DrawGameOverOverlay()
    {
        if (_sprites is null || _popupDelaySeconds > 0) return;

        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(40, 15, 15, (int)(235 * _overlayFadeIn)));

        var scale = _overlayScale;
        var centerX = screenW / 2;
        var centerY = screenH / 2 - 30;
        var panelW = (int)(420 * scale);
        var panelH = (int)(400 * scale);

        Raylib.DrawRectangle(centerX - panelW / 2 + 5, centerY - panelH / 2 + 5, panelW, panelH, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH, new Color(50, 25, 25, 250));
        Raylib.DrawRectangleLinesEx(new Rectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH), 3, Shot);

        if (scale > 0.5f)
        {
            DrawSpriteAtCenter(_sprites.Skull, centerX, centerY - panelH / 2 + 45, 56);
            var title = "GAME OVER";
            Raylib.DrawText(title, centerX - Raylib.MeasureText(title, 38) / 2, centerY - panelH / 2 + 85, 38, Shot);

            var statsY = centerY - panelH / 2 + 140;
            DrawStatLine(centerX, statsY, "Final Score", $"{_session.Rat.Score:N0}", Gold); statsY += 28;
            DrawStatLine(centerX, statsY, "Chapters Cleared", $"{_session.ChapterNumber - 1}", HudText); statsY += 28;
            DrawStatLine(centerX, statsY, "Gems Collected", $"{_session.Rat.GemsCollected}", Cyan); statsY += 28;
            DrawStatLine(centerX, statsY, "Max Combo", $"x{_session.Rat.MaxCombo}", _session.Rat.MaxCombo >= 10 ? Gold : HudText); statsY += 28;
            DrawStatLine(centerX, statsY, "Shots Dodged", $"{_session.Rat.ShotsAvoided}", Success);

            DrawButton(GetCenterButtonRect(0), "PLAY AGAIN", ButtonColor, ButtonHover);
            DrawButton(GetCenterButtonRect(1), "MAIN MENU", new Color(90, 90, 100, 255), new Color(110, 110, 120, 255));
        }
    }

    private void DrawStatLine(int centerX, int y, string label, string value, Color valueColor)
    {
        Raylib.DrawText($"{label}:", centerX - 90, y, 18, HudDim);
        Raylib.DrawText(value, centerX + 50, y, 20, valueColor);
    }

    private void DrawButton(Rectangle rect, string text, Color normalColor, Color hoverColor)
    {
        var mousePos = Raylib.GetMousePosition();
        var isHovered = CheckCollisionPointRec(mousePos, rect);
        Raylib.DrawRectangle((int)rect.X + 3, (int)rect.Y + 3, (int)rect.Width, (int)rect.Height, new Color(0, 0, 0, 50));
        Raylib.DrawRectangleRec(rect, isHovered ? hoverColor : normalColor);
        Raylib.DrawRectangleLinesEx(rect, 2, isHovered ? Color.White : new Color(200, 200, 210, 200));
        var textW = Raylib.MeasureText(text, 22);
        Raylib.DrawText(text, (int)(rect.X + rect.Width / 2 - textW / 2), (int)(rect.Y + rect.Height / 2 - 11), 22, Color.White);
    }

    private void SpawnCelebrationParticles()
    {
        var random = new Random();
        for (var i = 0; i < 2; i++)
            _particles.Add(new ParticleEffect { X = random.Next(0, Raylib.GetScreenWidth()), Y = -10, VelocityX = (float)(random.NextDouble() - 0.5) * 60, VelocityY = (float)(random.NextDouble() * 80 + 40), Color = (i % 3) switch { 0 => Gold, 1 => Success, _ => Cyan }, Size = random.Next(4, 9), LifeSeconds = 4f });
    }

    private void SpawnFloatingText(Position pos, string text, Color color)
    {
        _floatingTexts.Add(new FloatingText { X = Margin + (int)_shakeOffset.X + pos.X * TileSize + TileSize / 2, Y = Margin + (int)_shakeOffset.Y + pos.Y * TileSize, Text = text, Color = color, LifeSeconds = 1.2f, TotalLife = 1.2f });
    }

    private void HandleEvents(IReadOnlyList<GameEvent> events)
    {
        foreach (var e in events)
        {
            switch (e.Kind)
            {
                case GameEventKind.ShotWaveFired: if (e.Positions != null) foreach (var t in e.Positions) _effects.Add(new VisualEffect(VisualEffectKind.ShotImpact, t, 0.3f)); break;
                case GameEventKind.ShotHit: _ratHitFlashSecondsLeft = 0.28f; StartShake(0.25, 10); if (e.Position is { } hp) SpawnFloatingText(hp, $"-{e.Amount}", Shot); break;
                case GameEventKind.SnakeBite: if (e.Position is { } bp) { _effects.Add(new VisualEffect(VisualEffectKind.SnakeBite, bp, 0.35f)); SpawnFloatingText(bp, "-1", Shot); _consumedItems.Add(new ConsumedItem(bp, CellContent.Snake, 1.5f)); } _ratHitFlashSecondsLeft = 0.35f; StartShake(0.3, 10); break;
                case GameEventKind.GemFound: if (e.Position is { } gp) { _effects.Add(new VisualEffect(VisualEffectKind.GemSparkle, gp, 1.0f)); SpawnFloatingText(gp, "+100", Gold); _consumedItems.Add(new ConsumedItem(gp, CellContent.Gem, 2.0f)); } break;
                case GameEventKind.HealthPickup: if (e.Position is { } hp2) { _effects.Add(new VisualEffect(VisualEffectKind.HealthPickup, hp2, 0.5f)); SpawnFloatingText(hp2, "+HP", Success); _consumedItems.Add(new ConsumedItem(hp2, CellContent.HealthPickup, 1.5f)); } break;
                case GameEventKind.ShieldPickup: if (e.Position is { } sp) { _effects.Add(new VisualEffect(VisualEffectKind.ShieldPickup, sp, 0.6f)); SpawnFloatingText(sp, "SHIELD!", Cyan); _consumedItems.Add(new ConsumedItem(sp, CellContent.Shield, 1.5f)); } break;
                case GameEventKind.SpeedBoostPickup: if (e.Position is { } sbp) { _effects.Add(new VisualEffect(VisualEffectKind.SpeedBoostPickup, sbp, 0.5f)); SpawnFloatingText(sbp, "SPEED!", Gold); _consumedItems.Add(new ConsumedItem(sbp, CellContent.SpeedBoost, 1.5f)); } break;
                case GameEventKind.RockDug: if (e.Position is { } rp) { _effects.Add(new VisualEffect(VisualEffectKind.RockDug, rp, 0.45f)); SpawnFloatingText(rp, "DIG!", HudDim); } StartShake(0.18, 6); break;
                case GameEventKind.ComboMilestone: _effects.Add(new VisualEffect(VisualEffectKind.ComboFlash, _session.Rat.Position, 0.6f)); SpawnFloatingText(_session.Rat.Position, $"x{e.Amount}!", Gold); break;
                case GameEventKind.ShotsAvoided: if (e.Amount > 0) SpawnFloatingText(_session.Rat.Position, $"+{e.Amount * 5}", new Color(150, 200, 150, 255)); break;
            }
        }
    }

    private void UpdateEffects(float dt) { if (_ratHitFlashSecondsLeft > 0) _ratHitFlashSecondsLeft = Math.Max(0, _ratHitFlashSecondsLeft - dt); for (var i = _effects.Count - 1; i >= 0; i--) { _effects[i].TimeLeftSeconds -= dt; if (_effects[i].TimeLeftSeconds <= 0) _effects.RemoveAt(i); } }
    private void UpdateParticles(float dt) { for (var i = _particles.Count - 1; i >= 0; i--) { var p = _particles[i]; p.X += p.VelocityX * dt; p.Y += p.VelocityY * dt; p.VelocityY += 80 * dt; p.LifeSeconds -= dt; if (p.LifeSeconds <= 0 || p.Y > Raylib.GetScreenHeight() + 50) _particles.RemoveAt(i); } }
    private void UpdateFloatingTexts(float dt) { for (var i = _floatingTexts.Count - 1; i >= 0; i--) { _floatingTexts[i].Y -= 40 * dt; _floatingTexts[i].LifeSeconds -= dt; if (_floatingTexts[i].LifeSeconds <= 0) _floatingTexts.RemoveAt(i); } }
    private void UpdateConsumedItems(float dt) { for (var i = _consumedItems.Count - 1; i >= 0; i--) { _consumedItems[i].TimeLeftSeconds -= dt; if (_consumedItems[i].TimeLeftSeconds <= 0) _consumedItems.RemoveAt(i); } }
    private void DrawParticles() { foreach (var p in _particles) Raylib.DrawCircle((int)p.X, (int)p.Y, p.Size, new Color(p.Color.R, p.Color.G, p.Color.B, (byte)(255 * Math.Min(1, p.LifeSeconds / 0.5f)))); }
    private void DrawFloatingTexts() { foreach (var ft in _floatingTexts) { var progress = 1f - (ft.LifeSeconds / ft.TotalLife); Raylib.DrawText(ft.Text, (int)ft.X - Raylib.MeasureText(ft.Text, 18) / 2, (int)ft.Y, 18, new Color(ft.Color.R, ft.Color.G, ft.Color.B, (byte)(255 * (1f - progress * progress)))); } }

    private void DrawEffects(int gridOriginX, int gridOriginY)
    {
        foreach (var e in _effects)
        {
            var px = gridOriginX + e.Position.X * TileSize;
            var py = gridOriginY + e.Position.Y * TileSize;
            var t = e.TotalSeconds <= 0.0001f ? 0f : Math.Clamp(e.TimeLeftSeconds / e.TotalSeconds, 0f, 1f);
            switch (e.Kind)
            {
                case VisualEffectKind.ShotImpact: var exp = (1 - t) * 4; Raylib.DrawRectangle((int)(px + 2 - exp), (int)(py + 2 - exp), (int)(TileSize - 4 + exp * 2), (int)(TileSize - 4 + exp * 2), new Color(255, 70, 70, (int)(220 * t))); break;
                case VisualEffectKind.SnakeBite: Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * (0.3f + 0.25f * (1 - t)), new Color(80, 200, 100, (int)(220 * t))); break;
                case VisualEffectKind.GemSparkle: var r = (int)(TileSize * (0.25f + 0.4f * (1 - t))); Raylib.DrawLine(px + TileSize / 2 - r, py + TileSize / 2, px + TileSize / 2 + r, py + TileSize / 2, new Color(255, 255, 255, (int)(255 * t))); Raylib.DrawLine(px + TileSize / 2, py + TileSize / 2 - r, px + TileSize / 2, py + TileSize / 2 + r, new Color(255, 255, 255, (int)(255 * t))); break;
                case VisualEffectKind.HealthPickup: Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * (0.3f + 0.4f * (1 - t)), new Color(240, 80, 80, (int)(200 * t))); break;
                case VisualEffectKind.ShieldPickup: Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * (0.3f + 0.5f * (1 - t)), new Color(100, 180, 255, (int)(220 * t))); break;
                case VisualEffectKind.SpeedBoostPickup: var sr = (int)(TileSize * (0.3f + 0.3f * (1 - t))); Raylib.DrawLine(px + TileSize / 2 - sr, py + TileSize / 2 - sr, px + TileSize / 2, py + TileSize / 2, new Color(255, 230, 80, (int)(200 * t))); Raylib.DrawLine(px + TileSize / 2, py + TileSize / 2, px + TileSize / 2 + sr, py + TileSize / 2 + sr, new Color(255, 230, 80, (int)(200 * t))); break;
                case VisualEffectKind.RockDug: var sp = (1 - t) * TileSize * 0.7f; Raylib.DrawCircle((int)(px + TileSize / 2 - sp), (int)(py + TileSize / 2 - sp), 5, new Color(170, 170, 186, (int)(180 * t))); Raylib.DrawCircle((int)(px + TileSize / 2 + sp), (int)(py + TileSize / 2 - sp), 4, new Color(170, 170, 186, (int)(180 * t))); Raylib.DrawCircle((int)(px + TileSize / 2), (int)(py + TileSize / 2 + sp), 5, new Color(170, 170, 186, (int)(180 * t))); break;
                case VisualEffectKind.ComboFlash: Raylib.DrawCircleLines(px + TileSize / 2, py + TileSize / 2, TileSize * (0.5f + 0.8f * (1 - t)), new Color(255, 230, 100, (int)(180 * t))); break;
            }
        }
    }

    private void StartShake(double seconds, float strength) { _shakeSecondsLeft = Math.Max(_shakeSecondsLeft, seconds); _shakeStrength = Math.Max(_shakeStrength, strength); }
    private void UpdateShake(float dt) { if (_shakeSecondsLeft <= 0) { _shakeOffset = Vector2.Zero; _shakeStrength = 6f; return; } _shakeSecondsLeft = Math.Max(0, _shakeSecondsLeft - dt); var fade = (float)Math.Clamp(_shakeSecondsLeft / 0.30, 0.0, 1.0); _shakeOffset = new Vector2(MathF.Sin((float)_elapsedSeconds * 55f) * _shakeStrength * fade, MathF.Cos((float)_elapsedSeconds * 60f) * _shakeStrength * fade); }

    private enum GameState { MainMenu, Playing }
    private sealed class VisualEffect { public VisualEffect(VisualEffectKind kind, Position position, float seconds) { Kind = kind; Position = position; TotalSeconds = seconds; TimeLeftSeconds = seconds; } public VisualEffectKind Kind { get; } public Position Position { get; } public float TotalSeconds { get; } public float TimeLeftSeconds { get; set; } }
    private enum VisualEffectKind { ShotImpact, SnakeBite, GemSparkle, HealthPickup, ShieldPickup, SpeedBoostPickup, RockDug, ComboFlash }
    private sealed class ParticleEffect { public float X { get; set; } public float Y { get; set; } public float VelocityX { get; set; } public float VelocityY { get; set; } public Color Color { get; set; } public int Size { get; set; } public float LifeSeconds { get; set; } }
    private sealed class FloatingText { public float X { get; set; } public float Y { get; set; } public string Text { get; set; } = ""; public Color Color { get; set; } public float LifeSeconds { get; set; } public float TotalLife { get; set; } }
    private sealed class ConsumedItem { public ConsumedItem(Position pos, CellContent content, float seconds) { Position = pos; Content = content; TotalSeconds = seconds; TimeLeftSeconds = seconds; } public Position Position { get; } public CellContent Content { get; } public float TotalSeconds { get; } public float TimeLeftSeconds { get; set; } }
}
