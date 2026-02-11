using Rat.Game;
using Raylib_cs;
using System.Numerics;

namespace Rat.Desktop;

/// <summary>
/// Main application class for the RAT desktop game.
/// Handles game loop, state management, and input.
/// </summary>
internal sealed class RatDesktopApp
{
    private readonly int? _seed;
    
    // Core game components
    private RealTimeGameSession _session = null!;
    private SpriteTextures? _sprites;
    private GameRenderer? _renderer;
    private EffectsManager _effects = new();
    private readonly List<GameMessage> _messageLog = new();

    // Timing
    private double _elapsedSeconds;
    private float _menuAnimationTime;
    
    // State
    private GameState _gameState = GameState.MainMenu;
    private bool _exitRequested;
    
    // Overlay animation
    private float _overlayFadeIn;
    private float _overlayScale;
    private float _popupDelaySeconds;
    private bool _popupDelayStarted;
    
    // Level transition
    private float _levelTransitionAlpha;
    private bool _isTransitioning;

    public RatDesktopApp(int? seed)
    {
        _seed = seed;
    }

    /// <summary>
    /// Runs the main game loop.
    /// </summary>
    public void Run()
    {
        InitializeWindow();
        
        try
        {
            RunGameLoop();
        }
        finally
        {
            Cleanup();
        }
    }

    private void InitializeWindow()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(GameConstants.InitialWindowWidth, GameConstants.InitialWindowHeight, "RAT - Find the Gem!");
        Raylib.SetTargetFPS(GameConstants.TargetFps);
        Raylib.SetExitKey(KeyboardKey.Null);
        
        _sprites = new SpriteTextures();
        _renderer = new GameRenderer(_sprites);
    }

    private void RunGameLoop()
    {
        while (!Raylib.WindowShouldClose() && !_exitRequested)
        {
            var dt = Raylib.GetFrameTime();
            _elapsedSeconds += dt;
            _menuAnimationTime += dt;
            _renderer?.SetElapsedTime(_elapsedSeconds);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(GameColors.Background);

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

    private void Cleanup()
    {
        _sprites?.Dispose();
        _sprites = null;
        _renderer = null;
        Raylib.CloseWindow();
    }

    #region Game State Management

    private void StartNewGame()
    {
        _session = new RealTimeGameSession(seed: _seed);
        _messageLog.Clear();
        _effects.Clear();
        ResetOverlayState();
        _gameState = GameState.Playing;
        StartLevelTransition();
        ResizeWindowToLevel();
    }

    private void StartNextChapter()
    {
        _session.StartNextChapter();
        _messageLog.Clear();
        _effects.Clear();
        ResetOverlayState();
        StartLevelTransition();
        ResizeWindowToLevel();
    }

    private void ResetOverlayState()
    {
        _overlayFadeIn = 0;
        _overlayScale = 0;
        _popupDelayStarted = false;
        _popupDelaySeconds = 0;
    }

    private void StartLevelTransition()
    {
        _isTransitioning = true;
        _levelTransitionAlpha = 1f;
    }

    private void ResizeWindowToLevel()
    {
        var gridWidth = _session.Level.Width * GameConstants.TileSize;
        var gridHeight = _session.Level.Height * GameConstants.TileSize;
        var width = GameConstants.Margin * 3 + gridWidth + GameConstants.SidePanelWidth;
        var height = GameConstants.Margin * 2 + Math.Max(gridHeight, 500);
        Raylib.SetWindowSize(Math.Max(GameConstants.MinWindowWidth, width), Math.Max(GameConstants.MinWindowHeight, height));
    }

    #endregion

    #region Main Menu

    private void UpdateMainMenu()
    {
        var mousePos = Raylib.GetMousePosition();
        var clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        var playRect = GameRenderer.GetMenuButtonRect(0, screenW, screenH);
        if ((clicked && GameRenderer.CheckCollisionPointRec(mousePos, playRect)) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter) ||
            Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            StartNewGame();
        }

        var quitRect = GameRenderer.GetMenuButtonRect(1, screenW, screenH);
        if (clicked && GameRenderer.CheckCollisionPointRec(mousePos, quitRect))
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
        _renderer?.DrawMainMenu(_menuAnimationTime);
    }

    #endregion

    #region Game Update

    private void UpdateGame(float dt)
    {
        UpdateLevelTransition(dt);
        
        Direction? moveDirection = null;
        if (_session.Status == SessionStatus.InProgress)
            moveDirection = ReadMoveDirection();

        var outcome = _session.Update(dt, moveDirection);
        
        AppendMessages(outcome.Messages);
        _effects.HandleEvents(outcome.Events, _session.Rat.Position);
        _effects.Update(dt, _elapsedSeconds);
        UpdateOverlay(dt);
        HandleGameInput();
    }

    private void UpdateLevelTransition(float dt)
    {
        if (_isTransitioning)
        {
            _levelTransitionAlpha = Math.Max(0, _levelTransitionAlpha - dt * GameConstants.LevelTransitionSpeed);
            if (_levelTransitionAlpha <= 0)
                _isTransitioning = false;
        }
    }

    private void UpdateOverlay(float dt)
    {
        if (_session.Status == SessionStatus.InProgress)
            return;

        if (!_popupDelayStarted)
        {
            _popupDelayStarted = true;
            _popupDelaySeconds = _session.Status == SessionStatus.ChapterComplete
                ? GameConstants.ChapterCompleteDelay
                : GameConstants.GameOverDelay;
        }

        if (_popupDelaySeconds > 0)
        {
            _popupDelaySeconds -= dt;
            return;
        }

        _overlayFadeIn = Math.Min(1f, _overlayFadeIn + dt * GameConstants.OverlayFadeSpeed);
        _overlayScale = EaseOutBack(_overlayFadeIn);
        
        if (_session.Status == SessionStatus.ChapterComplete && _overlayFadeIn > 0.3f)
        {
            _effects.SpawnCelebrationParticles();
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
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
        
        foreach (var message in messages)
            _messageLog.Add(message);

        const int maxMessages = 8;
        while (_messageLog.Count > maxMessages)
            _messageLog.RemoveAt(0);
    }

    #endregion

    #region Game Input

    private void HandleGameInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _session.Status == SessionStatus.InProgress)
        {
            _gameState = GameState.MainMenu;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Q))
        {
            _gameState = GameState.MainMenu;
            return;
        }

        if (_session.Status == SessionStatus.InProgress)
            return;

        if (_popupDelaySeconds > 0)
            return;

        var mousePos = Raylib.GetMousePosition();
        var clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);

        if (_session.Status == SessionStatus.ChapterComplete)
        {
            HandleChapterCompleteInput(mousePos, clicked);
        }
        else if (_session.Status == SessionStatus.GameOver)
        {
            HandleGameOverInput(mousePos, clicked);
        }
    }

    private void HandleChapterCompleteInput(Vector2 mousePos, bool clicked)
    {
        var buttonRect = GameRenderer.GetCenterButtonRect(0);
        if ((clicked && GameRenderer.CheckCollisionPointRec(mousePos, buttonRect)) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter) ||
            Raylib.IsKeyPressed(KeyboardKey.Space) ||
            Raylib.IsKeyPressed(KeyboardKey.N))
        {
            StartNextChapter();
        }
    }

    private void HandleGameOverInput(Vector2 mousePos, bool clicked)
    {
        var playAgainRect = GameRenderer.GetCenterButtonRect(0);
        if ((clicked && GameRenderer.CheckCollisionPointRec(mousePos, playAgainRect)) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter) ||
            Raylib.IsKeyPressed(KeyboardKey.Space) ||
            Raylib.IsKeyPressed(KeyboardKey.R))
        {
            StartNewGame();
        }

        var menuRect = GameRenderer.GetCenterButtonRect(1);
        if (clicked && GameRenderer.CheckCollisionPointRec(mousePos, menuRect))
        {
            _gameState = GameState.MainMenu;
        }
    }

    #endregion

    #region Game Drawing

    private void DrawGame()
    {
        if (_renderer is null) return;

        var shakeOffset = _effects.ShakeOffset;
        var gridWidth = _session.Level.Width * GameConstants.TileSize;
        var gridHeight = _session.Level.Height * GameConstants.TileSize;

        _renderer.DrawGrid(_session, _effects);
        _renderer.DrawHud(_session, shakeOffset);
        _renderer.DrawMessages(_messageLog, gridWidth, shakeOffset);

        if (_session.Status == SessionStatus.ChapterComplete && _popupDelaySeconds <= 0)
        {
            _renderer.DrawChapterCompleteOverlay(_session, _overlayFadeIn, _overlayScale);
        }
        else if (_session.Status == SessionStatus.GameOver && _popupDelaySeconds <= 0)
        {
            _renderer.DrawGameOverOverlay(_session, _overlayFadeIn, _overlayScale);
        }
        else if (_session.Status == SessionStatus.InProgress)
        {
            _renderer.DrawInGameHints(gridWidth, gridHeight, shakeOffset);
        }

        _renderer.DrawFloatingTexts(_effects.FloatingTexts);

        if (_isTransitioning)
        {
            _renderer.DrawLevelTransition(_levelTransitionAlpha);
        }
    }

    #endregion
}
