using Rat.Game;
using Raylib_cs;
using System.Numerics;

namespace Rat.Desktop;

/// <summary>
/// Handles all rendering for the game.
/// </summary>
internal sealed class GameRenderer
{
    private readonly SpriteTextures _sprites;
    private double _elapsedSeconds;

    public GameRenderer(SpriteTextures sprites)
    {
        _sprites = sprites;
    }

    public void SetElapsedTime(double elapsed) => _elapsedSeconds = elapsed;

    #region Main Menu

    public void DrawMainMenu(float menuAnimationTime)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;

        DrawMenuBackground(menuAnimationTime);
        DrawMenuTitle(centerX, screenH, menuAnimationTime);
        DrawMenuCharacters(centerX, screenH, menuAnimationTime);
        DrawMenuButtons(screenW, screenH);
        DrawMenuInstructions(centerX, screenH);
        DrawVersion(screenH);
    }

    private void DrawMenuBackground(float menuAnimationTime)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var gridSize = 50;
        var offset = (int)(menuAnimationTime * 20) % gridSize;

        // Animated grid
        for (var x = -gridSize + offset; x < screenW + gridSize; x += gridSize)
        {
            for (var y = -gridSize + offset; y < screenH + gridSize; y += gridSize)
            {
                var alpha = (int)(20 + 10 * MathF.Sin((x + y) * 0.02f + menuAnimationTime));
                Raylib.DrawRectangleLines(x, y, gridSize - 2, gridSize - 2, new Color(50, 55, 70, alpha));
            }
        }

        // Floating particles
        for (var i = 0; i < 20; i++)
        {
            var px = (int)((MathF.Sin(menuAnimationTime * 0.5f + i * 0.7f) + 1) * 0.5f * screenW);
            var py = (int)((MathF.Cos(menuAnimationTime * 0.3f + i * 1.1f) + 1) * 0.5f * screenH);
            var size = 2 + (i % 3);
            var alpha = 30 + (i % 4) * 15;
            var color = i % 3 == 0 ? GameColors.Gold : (i % 3 == 1 ? GameColors.Cyan : GameColors.Success);
            Raylib.DrawCircle(px, py, size, GameColors.WithAlpha(color, alpha));
        }
    }

    private void DrawMenuTitle(int centerX, int screenH, float menuAnimationTime)
    {
        var titleY = screenH / 4;
        var titleBounce = MathF.Sin(menuAnimationTime * 2f) * 8f;

        var title = "RAT";
        var titleW = Raylib.MeasureText(title, 80);
        
        // Shadow
        Raylib.DrawText(title, centerX - titleW / 2 + 4, (int)(titleY + titleBounce) + 4, 80, new Color(0, 0, 0, 100));
        // Title
        Raylib.DrawText(title, centerX - titleW / 2, (int)(titleY + titleBounce), 80, GameColors.Gold);

        // Subtitle
        var subtitle = "Find the Gem!";
        var subtitleW = Raylib.MeasureText(subtitle, 28);
        Raylib.DrawText(subtitle, centerX - subtitleW / 2, titleY + 90, 28, GameColors.Cyan);
    }

    private void DrawMenuCharacters(int centerX, int screenH, float menuAnimationTime)
    {
        var titleY = screenH / 4;
        var ratY = titleY + 140;
        var ratBob = MathF.Sin(menuAnimationTime * 4f) * 3f;

        // Rat
        var ratFrame = ((int)(menuAnimationTime * 6) % 2) == 0 ? _sprites.RatA : _sprites.RatB;
        DrawSpriteAtCenter(ratFrame, centerX, (int)(ratY + ratBob), 64);

        // Gem
        var gemX = centerX + 50;
        var gemPulse = 1f + 0.1f * MathF.Sin(menuAnimationTime * 3f);
        var gemFrame = ((int)(menuAnimationTime * 3) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
        DrawSpriteAtCenter(gemFrame, gemX, (int)(ratY + ratBob), (int)(48 * gemPulse));
    }

    private void DrawMenuButtons(int screenW, int screenH)
    {
        var playRect = GetMenuButtonRect(0, screenW, screenH);
        var quitRect = GetMenuButtonRect(1, screenW, screenH);

        DrawButton(playRect, "PLAY", GameColors.ButtonSuccess, GameColors.ButtonSuccessHover);
        DrawButton(quitRect, "QUIT", GameColors.ButtonNeutral, GameColors.ButtonNeutralHover);
    }

    private void DrawMenuInstructions(int centerX, int screenH)
    {
        var instructY = screenH - 100;
        var instr1 = "Use WASD or Arrow Keys to move";
        var instr2 = "Avoid shots and snakes, find the gem!";
        var instr1W = Raylib.MeasureText(instr1, 16);
        var instr2W = Raylib.MeasureText(instr2, 16);
        Raylib.DrawText(instr1, centerX - instr1W / 2, instructY, 16, GameColors.HudDim);
        Raylib.DrawText(instr2, centerX - instr2W / 2, instructY + 22, 16, GameColors.HudDim);
    }

    private void DrawVersion(int screenH)
    {
        Raylib.DrawText("v1.0", 10, screenH - 25, 14, new Color(80, 80, 90, 255));
    }

    public static Rectangle GetMenuButtonRect(int index, int screenW, int screenH)
    {
        var centerX = screenW / 2;
        var buttonY = screenH / 2 + 80 + index * (GameConstants.ButtonHeight + 20);
        return new Rectangle(centerX - GameConstants.ButtonWidth / 2, buttonY, GameConstants.ButtonWidth, GameConstants.ButtonHeight);
    }

    #endregion

    #region Game Grid

    public void DrawGrid(RealTimeGameSession session, EffectsManager effects)
    {
        var shakeOffset = effects.ShakeOffset;
        var gridOriginX = GameConstants.Margin + (int)shakeOffset.X;
        var gridOriginY = GameConstants.Margin + (int)shakeOffset.Y;
        var level = session.Level;
        var gridWidth = level.Width * GameConstants.TileSize;
        var gridHeight = level.Height * GameConstants.TileSize;

        // Background
        Raylib.DrawRectangle(gridOriginX - 6, gridOriginY - 6, gridWidth + 12, gridHeight + 12, new Color(15, 17, 22, 255));
        Raylib.DrawRectangle(gridOriginX - 3, gridOriginY - 3, gridWidth + 6, gridHeight + 6, GameColors.GridBackground);

        var revealAll = session.Status is SessionStatus.ChapterComplete or SessionStatus.GameOver;
        var shotTargets = session.TelegraphedShots.Select(s => s.Target).ToHashSet();
        var ratTargeted = shotTargets.Contains(session.Rat.Position);

        var timeRatio = session.ShotTelegraphSeconds <= 0.0001 ? 0f : (float)(session.TimeUntilShotsFireSeconds / session.ShotTelegraphSeconds);
        var urgency = 1f - Math.Clamp(timeRatio, 0f, 1f);
        var pulse = 0.4f + 0.6f * MathF.Abs(MathF.Sin((float)_elapsedSeconds * (3f + urgency * 10f)));

        // Draw tiles
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var pos = new Position(x, y);
                var cell = level.GetCell(pos);
                var px = gridOriginX + x * GameConstants.TileSize;
                var py = gridOriginY + y * GameConstants.TileSize;

                DrawTile(cell, px, py, x, y, revealAll);
                
                if (shotTargets.Contains(pos))
                    DrawCrosshair(px, py, pulse, urgency);

                if (!cell.IsDug && !revealAll)
                {
                    if (session.Rat.Position == pos)
                        DrawRat(px, py, ratTargeted, session.Rat, effects.RatHitFlash);
                    continue;
                }

                DrawCellContent(cell.Content, px, py);

                if (session.Rat.Position == pos)
                    DrawRat(px, py, ratTargeted, session.Rat, effects.RatHitFlash);
            }
        }

        // Draw effects
        DrawParticles(effects.Particles);
        DrawEffects(effects.Effects, gridOriginX, gridOriginY);
        DrawConsumedItems(effects.ConsumedItems, gridOriginX, gridOriginY);

        // Gem indicator
        if (revealAll || level.GetCell(level.GemPosition).IsDug)
            DrawGemIndicator(level.GemPosition, gridOriginX, gridOriginY);
    }

    private void DrawTile(Cell cell, int px, int py, int x, int y, bool revealAll)
    {
        var baseColor = cell.IsDug ? GameColors.Dug : (revealAll ? GameColors.DustRevealed : GameColors.Dust);
        Raylib.DrawRectangle(px, py, GameConstants.TileSize, GameConstants.TileSize, baseColor);

        if (!cell.IsDug && !revealAll && (x + y) % 2 == 0)
            Raylib.DrawRectangle(px + 2, py + 2, GameConstants.TileSize - 4, GameConstants.TileSize - 4, GameColors.DustPattern);

        Raylib.DrawRectangleLines(px, py, GameConstants.TileSize, GameConstants.TileSize, GameColors.GridLine);
    }

    private void DrawCellContent(CellContent content, int px, int py)
    {
        switch (content)
        {
            case CellContent.Rock:
                DrawSprite(_sprites.Rock, px, py, 1.0f);
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
        }
    }

    private void DrawCrosshair(int px, int py, float pulse, float urgency)
    {
        var scale = 0.85f + 0.20f * pulse;
        var alpha = (int)(160 + 95 * pulse);
        DrawSprite(_sprites.Crosshair, px, py, scale, GameColors.WithAlpha(Color.White, alpha));
        
        var glowAlpha = (int)(30 + 50 * urgency * pulse);
        Raylib.DrawRectangle(px + 2, py + 2, GameConstants.TileSize - 4, GameConstants.TileSize - 4, 
            GameColors.WithAlpha(GameColors.DangerGlow, glowAlpha));
    }

    private void DrawRat(int px, int py, bool targeted, Game.Rat rat, float hitFlash)
    {
        var frame = ((int)(_elapsedSeconds * GameConstants.RatAnimationSpeed) % 2) == 0 ? _sprites.RatA : _sprites.RatB;
        var flash = hitFlash <= 0 ? 0f : Math.Clamp(hitFlash / 0.22f, 0f, 1f);
        var tint = targeted || flash > 0 
            ? new Color(255, (int)(130 + 90 * (1 - flash)), (int)(130 + 90 * (1 - flash)), 255) 
            : Color.White;
        
        var bob = MathF.Sin((float)_elapsedSeconds * 5f) * 1.5f;
        DrawSprite(frame, px, (int)(py - bob), 1.15f, tint);

        // Danger indicator
        if (targeted)
        {
            var dangerPulse = MathF.Abs(MathF.Sin((float)_elapsedSeconds * 10f));
            var dangerAlpha = (int)(120 + 135 * dangerPulse);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(px + 1, py + 1, GameConstants.TileSize - 2, GameConstants.TileSize - 2), 
                3, GameColors.WithAlpha(GameColors.Shot, dangerAlpha));
        }

        // Shield aura
        if (rat.HasShield)
        {
            var shieldPulse = 0.6f + 0.4f * MathF.Sin((float)_elapsedSeconds * 5f);
            Raylib.DrawCircleLines(px + GameConstants.TileSize / 2, py + GameConstants.TileSize / 2, 
                GameConstants.TileSize * 0.65f, GameColors.WithAlpha(GameColors.ShieldAura, shieldPulse));
        }

        // Speed boost effect
        if (rat.HasSpeedBoost)
        {
            var speedPulse = MathF.Abs(MathF.Sin((float)_elapsedSeconds * 12f));
            var speedColor = GameColors.WithAlpha(GameColors.SpeedAura, speedPulse * 0.6f);
            Raylib.DrawCircle(px + GameConstants.TileSize / 2 - 12, py + GameConstants.TileSize / 2, 5, speedColor);
            Raylib.DrawCircle(px + GameConstants.TileSize / 2 + 12, py + GameConstants.TileSize / 2, 5, speedColor);
        }
    }

    private void DrawSnake(int px, int py)
    {
        var frame = ((int)(_elapsedSeconds * GameConstants.SnakeAnimationSpeed) % 2) == 0 ? _sprites.SnakeA : _sprites.SnakeB;
        var sway = MathF.Sin((float)_elapsedSeconds * 3f + px * 0.1f) * 1.5f;
        DrawSprite(frame, (int)(px + sway), py, 1.0f);
    }

    private void DrawGem(int px, int py)
    {
        var frame = ((int)(_elapsedSeconds * GameConstants.GemAnimationSpeed) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
        var pulse = 1.0f + 0.12f * MathF.Sin((float)_elapsedSeconds * 5f);
        var bob = MathF.Sin((float)_elapsedSeconds * 3f) * 2f;
        DrawSprite(frame, px, (int)(py - bob), pulse);

        // Glow
        var glowPulse = 0.5f + 0.5f * MathF.Sin((float)_elapsedSeconds * 3f);
        Raylib.DrawCircle(px + GameConstants.TileSize / 2, py + GameConstants.TileSize / 2, 
            GameConstants.TileSize * 0.55f, GameColors.WithAlpha(GameColors.GemGlow, glowPulse * 0.4f));
    }

    private void DrawHealthPickup(int px, int py)
    {
        var pulse = 0.95f + 0.10f * MathF.Sin((float)_elapsedSeconds * 4f);
        var bob = MathF.Sin((float)_elapsedSeconds * 2.5f) * 2f;
        DrawSprite(_sprites.HealthPickup, px, (int)(py - bob), pulse);
    }

    private void DrawShield(int px, int py)
    {
        var pulse = 0.95f + 0.10f * MathF.Sin((float)_elapsedSeconds * 3f);
        var bob = MathF.Sin((float)_elapsedSeconds * 2f) * 2f;
        var glow = (int)(200 + 55 * MathF.Sin((float)_elapsedSeconds * 4f));
        DrawSprite(_sprites.Shield, px, (int)(py - bob), pulse, GameColors.WithAlpha(Color.White, glow));
    }

    private void DrawSpeedBoost(int px, int py)
    {
        var pulse = 0.92f + 0.15f * MathF.Sin((float)_elapsedSeconds * 6f);
        var bob = MathF.Sin((float)_elapsedSeconds * 3f) * 2f;
        DrawSprite(_sprites.SpeedBoost, px, (int)(py - bob), pulse);
    }

    private void DrawGemIndicator(Position gemPos, int gridOriginX, int gridOriginY)
    {
        var px = gridOriginX + gemPos.X * GameConstants.TileSize;
        var py = gridOriginY + gemPos.Y * GameConstants.TileSize;
        var glowPulse = 0.5f + 0.5f * MathF.Sin((float)_elapsedSeconds * 3f);

        for (var i = 4; i >= 1; i--)
        {
            var alpha = (int)(40 * glowPulse / i);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(px - i * 3, py - i * 3, GameConstants.TileSize + i * 6, GameConstants.TileSize + i * 6), 
                2, GameColors.WithAlpha(GameColors.GemGlow, alpha));
        }
    }

    #endregion

    #region Effects

    private void DrawParticles(IReadOnlyList<ParticleEffect> particles)
    {
        foreach (var p in particles)
        {
            var color = GameColors.WithAlpha(p.Color, p.Alpha);
            Raylib.DrawCircle((int)p.X, (int)p.Y, p.Size, color);
        }
    }

    public void DrawFloatingTexts(IReadOnlyList<FloatingText> floatingTexts)
    {
        foreach (var ft in floatingTexts)
        {
            var color = GameColors.WithAlpha(ft.Color, ft.Alpha);
            var textW = Raylib.MeasureText(ft.Text, 18);
            Raylib.DrawText(ft.Text, (int)ft.X - textW / 2, (int)ft.Y, 18, color);
        }
    }

    private void DrawEffects(IReadOnlyList<VisualEffect> effects, int gridOriginX, int gridOriginY)
    {
        foreach (var e in effects)
        {
            var px = gridOriginX + e.Position.X * GameConstants.TileSize;
            var py = gridOriginY + e.Position.Y * GameConstants.TileSize;
            var t = e.Progress;
            var center = GameConstants.TileSize / 2;

            switch (e.Kind)
            {
                case VisualEffectKind.ShotImpact:
                    DrawShotImpactEffect(px, py, t);
                    break;
                case VisualEffectKind.SnakeBite:
                    DrawCircleEffect(px + center, py + center, 0.3f, 0.25f, t, new Color(80, 200, 100, 255));
                    break;
                case VisualEffectKind.GemSparkle:
                    DrawSparkleEffect(px + center, py + center, t);
                    break;
                case VisualEffectKind.HealthPickup:
                    DrawCircleEffect(px + center, py + center, 0.3f, 0.4f, t, new Color(240, 80, 80, 255));
                    break;
                case VisualEffectKind.ShieldPickup:
                    DrawCircleEffect(px + center, py + center, 0.3f, 0.5f, t, GameColors.ShieldAura);
                    break;
                case VisualEffectKind.SpeedBoostPickup:
                    DrawSpeedBoostEffect(px + center, py + center, t);
                    break;
                case VisualEffectKind.RockDug:
                    DrawRockDugEffect(px + center, py + center, t);
                    break;
                case VisualEffectKind.ComboFlash:
                    DrawCircleEffect(px + center, py + center, 0.5f, 0.8f, t, new Color(255, 230, 100, 255));
                    break;
            }
        }
    }

    private void DrawShotImpactEffect(int px, int py, float t)
    {
        var expand = (1 - t) * 4;
        var alpha = (int)(220 * t);
        Raylib.DrawRectangle(
            (int)(px + 2 - expand), (int)(py + 2 - expand),
            (int)(GameConstants.TileSize - 4 + expand * 2), (int)(GameConstants.TileSize - 4 + expand * 2),
            GameColors.WithAlpha(GameColors.Shot, alpha));
    }

    private void DrawCircleEffect(int cx, int cy, float baseRadius, float expandRadius, float t, Color color)
    {
        var radius = GameConstants.TileSize * (baseRadius + expandRadius * (1 - t));
        var alpha = (int)(220 * t);
        Raylib.DrawCircleLines(cx, cy, radius, GameColors.WithAlpha(color, alpha));
    }

    private void DrawSparkleEffect(int cx, int cy, float t)
    {
        var r = (int)(GameConstants.TileSize * (0.25f + 0.4f * (1 - t)));
        var alpha = (int)(255 * t);
        var color = GameColors.WithAlpha(Color.White, alpha);
        Raylib.DrawLine(cx - r, cy, cx + r, cy, color);
        Raylib.DrawLine(cx, cy - r, cx, cy + r, color);
    }

    private void DrawSpeedBoostEffect(int cx, int cy, float t)
    {
        var r = (int)(GameConstants.TileSize * (0.3f + 0.3f * (1 - t)));
        var alpha = (int)(200 * t);
        var color = GameColors.WithAlpha(GameColors.SpeedAura, alpha);
        Raylib.DrawLine(cx - r, cy - r, cx, cy, color);
        Raylib.DrawLine(cx, cy, cx + r, cy + r, color);
    }

    private void DrawRockDugEffect(int cx, int cy, float t)
    {
        var spread = (1 - t) * GameConstants.TileSize * 0.7f;
        var alpha = (int)(180 * t);
        var color = new Color(170, 170, 186, alpha);
        Raylib.DrawCircle((int)(cx - spread), (int)(cy - spread), 5, color);
        Raylib.DrawCircle((int)(cx + spread), (int)(cy - spread), 4, color);
        Raylib.DrawCircle(cx, (int)(cy + spread), 5, color);
    }

    private void DrawConsumedItems(IReadOnlyList<ConsumedItem> consumedItems, int gridOriginX, int gridOriginY)
    {
        foreach (var item in consumedItems)
        {
            var px = gridOriginX + item.Position.X * GameConstants.TileSize;
            var py = gridOriginY + item.Position.Y * GameConstants.TileSize;
            var tint = GameColors.WithAlpha(Color.White, item.Alpha);

            switch (item.Content)
            {
                case CellContent.Snake:
                    var snakeFrame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.SnakeA : _sprites.SnakeB;
                    DrawSprite(snakeFrame, px, py, item.Scale, tint);
                    break;
                case CellContent.Gem:
                    var gemFrame = ((int)(_elapsedSeconds * 4) % 2) == 0 ? _sprites.GemA : _sprites.GemB;
                    DrawSprite(gemFrame, px, py, item.Scale, tint);
                    break;
                case CellContent.HealthPickup:
                    DrawSprite(_sprites.HealthPickup, px, py, item.Scale, tint);
                    break;
                case CellContent.Shield:
                    DrawSprite(_sprites.Shield, px, py, item.Scale, tint);
                    break;
                case CellContent.SpeedBoost:
                    DrawSprite(_sprites.SpeedBoost, px, py, item.Scale, tint);
                    break;
            }
        }
    }

    #endregion

    #region HUD

    public void DrawHud(RealTimeGameSession session, Vector2 shakeOffset)
    {
        var gridWidth = session.Level.Width * GameConstants.TileSize;
        var panelX = GameConstants.Margin * 2 + gridWidth + (int)shakeOffset.X;
        var panelY = GameConstants.Margin;
        var panelHeight = Math.Max(session.Level.Height * GameConstants.TileSize, 520);
        var panelWidth = GameConstants.SidePanelWidth;

        // Modern panel with gradient effect
        DrawModernPanel(panelX, panelY, panelWidth, panelHeight);

        var x = panelX + 16;
        var y = panelY + 12;

        // Header with title and chapter badge
        y = DrawModernHeader(x, y, panelX, panelWidth, session.ChapterNumber);

        // Score card
        y = DrawScoreCard(x, y, panelX, panelWidth, session.Rat);

        // Gem Compass - the strategic thinking element
        y = DrawGemCompass(x, y, panelX, panelWidth, session);

        // Health bar (modern style)
        y = DrawModernHealthBar(x, y, panelX, panelWidth, session.Rat);

        // Active power-ups
        y = DrawActivePowerUps(x, y, panelX, panelWidth, session.Rat);

        // Danger indicator
        DrawDangerIndicator(x, y + 8, panelX, panelWidth, session);
    }

    private void DrawModernPanel(int panelX, int panelY, int panelWidth, int panelHeight)
    {
        // Outer shadow
        Raylib.DrawRectangle(panelX + 4, panelY + 4, panelWidth, panelHeight, new Color(0, 0, 0, 40));
        
        // Main panel with gradient-like effect
        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, new Color(22, 24, 32, 250));
        
        // Top highlight
        Raylib.DrawRectangle(panelX, panelY, panelWidth, 3, new Color(60, 65, 85, 255));
        
        // Subtle inner border
        Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelWidth, panelHeight), 1, new Color(45, 50, 65, 255));
    }

    private int DrawModernHeader(int x, int y, int panelX, int panelWidth, int chapterNumber)
    {
        // Game title with glow
        var titlePulse = 0.85f + 0.15f * MathF.Sin((float)_elapsedSeconds * 2f);
        Raylib.DrawText("RAT", x, y, 36, GameColors.WithAlpha(GameColors.Gold, titlePulse));
        
        // Chapter badge
        var chapterText = $"CH {chapterNumber}";
        var badgeX = panelX + panelWidth - 70;
        var badgeColor = chapterNumber >= 5 ? GameColors.Gold : chapterNumber >= 3 ? GameColors.Cyan : new Color(80, 90, 110, 255);
        
        Raylib.DrawRectangleRounded(new Rectangle(badgeX, y + 4, 54, 28), 0.3f, 8, badgeColor);
        var textW = Raylib.MeasureText(chapterText, 16);
        Raylib.DrawText(chapterText, badgeX + 27 - textW / 2, y + 10, 16, Color.White);

        // Separator line
        y += 48;
        Raylib.DrawRectangle(panelX + 12, y, panelWidth - 24, 1, new Color(50, 55, 70, 255));
        
        return y + 12;
    }

    private int DrawScoreCard(int x, int y, int panelX, int panelWidth, Game.Rat rat)
    {
        // Score section with card style
        var cardWidth = panelWidth - 32;
        Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, cardWidth, 65), 0.1f, 8, new Color(30, 33, 42, 255));
        
        // Score
        Raylib.DrawText("SCORE", x, y + 8, 11, GameColors.HudDim);
        var scorePulse = rat.ComboCount > 0 ? 1f + 0.03f * MathF.Sin((float)_elapsedSeconds * 5f) : 1f;
        Raylib.DrawText($"{rat.Score:N0}", x, y + 22, (int)(28 * scorePulse), GameColors.Gold);

        // Combo (if active)
        if (rat.ComboCount > 0)
        {
            var comboX = panelX + panelWidth - 80;
            var comboPulse = rat.ComboCount >= 10 ? 1f + 0.15f * MathF.Sin((float)_elapsedSeconds * 8f) : 1f;
            var comboColor = rat.ComboCount >= 10 ? GameColors.Gold : rat.ComboCount >= 5 ? GameColors.Cyan : GameColors.Success;
            
            Raylib.DrawText("COMBO", comboX, y + 8, 10, GameColors.HudDim);
            Raylib.DrawText($"x{rat.ComboCount}", comboX, y + 22, (int)(24 * comboPulse), comboColor);
            
            // Combo fire effect
            if (rat.ComboCount >= 5)
            {
                var fireAlpha = (int)(80 + 40 * MathF.Sin((float)_elapsedSeconds * 10f));
                Raylib.DrawCircle(comboX + 20, y + 45, 15, GameColors.WithAlpha(comboColor, fireAlpha / 255f * 0.3f));
            }
        }

        return y + 75;
    }

    private int DrawGemCompass(int x, int y, int panelX, int panelWidth, RealTimeGameSession session)
    {
        var cardWidth = panelWidth - 32;
        var cardHeight = 100;
        
        // Card background
        Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, cardWidth, cardHeight), 0.1f, 8, new Color(25, 35, 45, 255));
        Raylib.DrawRectangleRoundedLines(new Rectangle(panelX + 12, y, cardWidth, cardHeight), 0.1f, 8, 1, GameColors.WithAlpha(GameColors.Cyan, 0.3f));
        
        // Title
        Raylib.DrawText("GEM RADAR", x, y + 8, 11, GameColors.Cyan);
        
        // Get gem info
        var distance = session.Level.GetDistanceToGem(session.Rat.Position);
        var temperature = session.Level.GetGemTemperature(session.Rat.Position);
        var (dirH, dirV) = session.Level.GetGemDirectionHint(session.Rat.Position);
        
        // Compass center
        var compassCenterX = panelX + 60;
        var compassCenterY = y + 58;
        var compassRadius = 28;
        
        // Draw compass background
        Raylib.DrawCircle(compassCenterX, compassCenterY, compassRadius + 4, new Color(15, 20, 28, 255));
        Raylib.DrawCircleLines(compassCenterX, compassCenterY, compassRadius, GameColors.WithAlpha(GameColors.Cyan, 0.4f));
        
        // Draw cardinal directions
        var dirColor = GameColors.WithAlpha(GameColors.HudDim, 0.5f);
        Raylib.DrawText("N", compassCenterX - 4, compassCenterY - compassRadius - 12, 10, dirColor);
        Raylib.DrawText("S", compassCenterX - 4, compassCenterY + compassRadius + 4, 10, dirColor);
        Raylib.DrawText("W", compassCenterX - compassRadius - 10, compassCenterY - 5, 10, dirColor);
        Raylib.DrawText("E", compassCenterX + compassRadius + 4, compassCenterY - 5, 10, dirColor);
        
        // Draw direction indicator (arrow pointing to gem)
        if (distance > 0)
        {
            var arrowPulse = 0.7f + 0.3f * MathF.Sin((float)_elapsedSeconds * 4f);
            var arrowColor = GameColors.WithAlpha(GameColors.Cyan, arrowPulse);
            
            // Calculate arrow direction
            var arrowLen = compassRadius - 8;
            var targetX = compassCenterX + dirH * arrowLen;
            var targetY = compassCenterY + dirV * arrowLen;
            
            if (dirH != 0 || dirV != 0)
            {
                // Normalize for diagonal
                if (dirH != 0 && dirV != 0)
                {
                    targetX = compassCenterX + (int)(dirH * arrowLen * 0.7f);
                    targetY = compassCenterY + (int)(dirV * arrowLen * 0.7f);
                }
                
                Raylib.DrawLineEx(new Vector2(compassCenterX, compassCenterY), new Vector2(targetX, targetY), 3, arrowColor);
                Raylib.DrawCircle(targetX, targetY, 5, arrowColor);
            }
        }
        else
        {
            // Gem found - draw gem icon
            var gemPulse = 1f + 0.2f * MathF.Sin((float)_elapsedSeconds * 6f);
            Raylib.DrawCircle(compassCenterX, compassCenterY, (int)(12 * gemPulse), GameColors.Cyan);
        }
        
        // Temperature bar (hot/cold indicator)
        var tempBarX = panelX + 110;
        var tempBarY = y + 28;
        var tempBarW = cardWidth - 110;
        var tempBarH = 12;
        
        Raylib.DrawText("DISTANCE", tempBarX, y + 8, 10, GameColors.HudDim);
        
        // Background
        Raylib.DrawRectangleRounded(new Rectangle(tempBarX, tempBarY, tempBarW, tempBarH), 0.5f, 8, new Color(20, 22, 30, 255));
        
        // Temperature fill with gradient effect
        var fillW = (int)(tempBarW * temperature);
        var tempColor = GetTemperatureColor(temperature);
        if (fillW > 0)
        {
            Raylib.DrawRectangleRounded(new Rectangle(tempBarX, tempBarY, fillW, tempBarH), 0.5f, 8, tempColor);
        }
        
        // Distance text
        var distText = distance == 0 ? "FOUND!" : $"{distance} tiles away";
        var distColor = distance == 0 ? GameColors.Success : distance <= 3 ? GameColors.Gold : GameColors.HudText;
        Raylib.DrawText(distText, tempBarX, tempBarY + 18, 14, distColor);
        
        // Hint text
        var hintText = GetDirectionHintText(dirH, dirV, distance);
        Raylib.DrawText(hintText, tempBarX, tempBarY + 36, 12, GameColors.HudDim);
        
        return y + cardHeight + 10;
    }

    private static Color GetTemperatureColor(float temp)
    {
        if (temp > 0.8f) return new Color(255, 80, 80, 255);  // Hot red
        if (temp > 0.6f) return new Color(255, 160, 60, 255); // Warm orange
        if (temp > 0.4f) return new Color(255, 220, 80, 255); // Yellow
        if (temp > 0.2f) return new Color(100, 200, 255, 255); // Cool blue
        return new Color(80, 140, 200, 255); // Cold blue
    }

    private static string GetDirectionHintText(int h, int v, int distance)
    {
        if (distance == 0) return "You found the gem!";
        
        var vertical = v < 0 ? "North" : v > 0 ? "South" : "";
        var horizontal = h < 0 ? "West" : h > 0 ? "East" : "";
        
        if (string.IsNullOrEmpty(vertical) && string.IsNullOrEmpty(horizontal))
            return "You're on target!";
        
        var dir = string.IsNullOrEmpty(vertical) ? horizontal : 
                  string.IsNullOrEmpty(horizontal) ? vertical : $"{vertical}-{horizontal}";
        
        return $"Head {dir}";
    }

    private int DrawModernHealthBar(int x, int y, int panelX, int panelWidth, Game.Rat rat)
    {
        var cardWidth = panelWidth - 32;
        
        Raylib.DrawText("HEALTH", x, y, 11, GameColors.HudDim);
        y += 16;
        
        // Health bar background
        var barWidth = cardWidth - 8;
        var barHeight = 20;
        Raylib.DrawRectangleRounded(new Rectangle(panelX + 16, y, barWidth, barHeight), 0.3f, 8, new Color(30, 20, 25, 255));
        
        // Health fill
        var healthRatio = (float)rat.Health / rat.MaxHealth;
        var healthWidth = (int)(barWidth * healthRatio);
        var healthColor = healthRatio > 0.6f ? GameColors.Success : healthRatio > 0.3f ? GameColors.Gold : GameColors.Shot;
        
        if (healthWidth > 0)
        {
            // Pulsing effect when low health
            if (healthRatio <= 0.3f)
            {
                var pulse = 0.7f + 0.3f * MathF.Sin((float)_elapsedSeconds * 8f);
                healthColor = GameColors.WithAlpha(healthColor, pulse);
            }
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 16, y, healthWidth, barHeight), 0.3f, 8, healthColor);
        }
        
        // Health text overlay
        var healthText = $"{rat.Health}/{rat.MaxHealth}";
        var textW = Raylib.MeasureText(healthText, 14);
        Raylib.DrawText(healthText, panelX + 16 + barWidth / 2 - textW / 2, y + 3, 14, Color.White);
        
        // Heart icons below the bar
        y += 26;
        for (var i = 0; i < rat.MaxHealth; i++)
        {
            var tex = i < rat.Health ? _sprites.HeartFull : _sprites.HeartEmpty;
            var heartPulse = i < rat.Health ? (1f + 0.05f * MathF.Sin((float)_elapsedSeconds * 3f + i * 0.5f)) : 1f;
            DrawSpriteAt(tex, panelX + 16 + i * 24, y, (int)(18 * heartPulse), Color.White);
        }
        
        return y + 28;
    }

    private int DrawActivePowerUps(int x, int y, int panelX, int panelWidth, Game.Rat rat)
    {
        if (!rat.HasShield && !rat.HasSpeedBoost && rat.RockDigsRemaining <= 0)
            return y;

        Raylib.DrawText("ACTIVE BUFFS", x, y, 11, GameColors.HudDim);
        y += 18;

        var cardWidth = panelWidth - 32;

        if (rat.HasShield)
        {
            var shieldPulse = 0.8f + 0.2f * MathF.Sin((float)_elapsedSeconds * 4f);
            var progress = (float)(rat.ShieldSecondsRemaining / 5.0); // Assume 5s max
            
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, cardWidth, 24), 0.2f, 8, new Color(30, 50, 80, 200));
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, (int)(cardWidth * Math.Min(1, progress)), 24), 0.2f, 8, 
                GameColors.WithAlpha(GameColors.ShieldAura, 0.4f));
            
            Raylib.DrawText($"🛡 SHIELD  {rat.ShieldSecondsRemaining:0.0}s", x, y + 5, 14, 
                GameColors.WithAlpha(GameColors.ShieldAura, shieldPulse));
            y += 28;
        }

        if (rat.HasSpeedBoost)
        {
            var speedPulse = 0.8f + 0.2f * MathF.Sin((float)_elapsedSeconds * 6f);
            var progress = (float)(rat.SpeedBoostSecondsRemaining / 5.0);
            
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, cardWidth, 24), 0.2f, 8, new Color(60, 50, 25, 200));
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, (int)(cardWidth * Math.Min(1, progress)), 24), 0.2f, 8, 
                GameColors.WithAlpha(GameColors.SpeedAura, 0.4f));
            
            Raylib.DrawText($"⚡ SPEED  {rat.SpeedBoostSecondsRemaining:0.0}s", x, y + 5, 14, 
                GameColors.WithAlpha(GameColors.SpeedAura, speedPulse));
            y += 28;
        }

        if (rat.RockDigsRemaining > 0)
        {
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, cardWidth, 24), 0.2f, 8, new Color(50, 45, 40, 200));
            Raylib.DrawText($"⛏ ROCK DIG x{rat.RockDigsRemaining}", x, y + 5, 14, GameColors.HudDim);
            y += 28;
        }

        return y + 4;
    }

    private void DrawDangerIndicator(int x, int y, int panelX, int panelWidth, RealTimeGameSession session)
    {
        var cardWidth = panelWidth - 32;
        var timeRatio = session.ShotTelegraphSeconds <= 0.0001 ? 0f 
            : (float)(session.TimeUntilShotsFireSeconds / session.ShotTelegraphSeconds);
        var dangerLevel = 1f - Math.Clamp(timeRatio, 0f, 1f);
        var dangerPulse = dangerLevel > 0.7f ? (0.7f + 0.3f * MathF.Sin((float)_elapsedSeconds * 12f)) : 1f;

        // Background with danger tint
        var bgRed = (byte)(35 + (int)(40 * dangerLevel));
        Raylib.DrawRectangleRounded(new Rectangle(panelX + 12, y, cardWidth, 75), 0.1f, 8, new Color(bgRed, (byte)25, (byte)30, (byte)250));
        
        if (dangerLevel > 0.5f)
        {
            Raylib.DrawRectangleRoundedLines(new Rectangle(panelX + 12, y, cardWidth, 75), 0.1f, 8, 2, 
                GameColors.WithAlpha(GameColors.Shot, dangerPulse * 0.8f));
        }

        y += 10;
        
        // Warning icon and text
        var warningText = dangerLevel > 0.8f ? "⚠ DANGER!" : "INCOMING";
        var warningColor = dangerLevel > 0.8f ? GameColors.WithAlpha(GameColors.Shot, dangerPulse) : GameColors.Shot;
        Raylib.DrawText(warningText, x, y, 12, warningColor);

        y += 18;

        var time = Math.Max(0, session.TimeUntilShotsFireSeconds);
        var timeText = $"{session.ChapterSettings.ShotsPerTurn} shots in {time:0.0}s";
        var timeColor = time < 1.0 
            ? GameColors.WithAlpha(new Color(255, 150, 150, 255), dangerPulse)
            : GameColors.HudText;
        Raylib.DrawText(timeText, x, y, 16, timeColor);

        y += 24;

        // Progress bar
        var barW = cardWidth - 16;
        Raylib.DrawRectangleRounded(new Rectangle(panelX + 20, y, barW, 10), 0.5f, 8, new Color(20, 15, 18, 255));
        
        var fill = (int)(barW * Math.Clamp(timeRatio, 0f, 1f));
        if (fill > 0)
        {
            var barColor = time < 1.0 
                ? GameColors.WithAlpha(GameColors.Shot, dangerPulse)
                : new Color(255, 140, 60, 255);
            Raylib.DrawRectangleRounded(new Rectangle(panelX + 20, y, fill, 10), 0.5f, 8, barColor);
        }
    }

    public void DrawMessages(IReadOnlyList<GameMessage> messageLog, int gridWidth, Vector2 shakeOffset)
    {
        if (messageLog.Count == 0) return;

        var x = GameConstants.Margin * 2 + gridWidth + (int)shakeOffset.X + 12;
        var startY = GameConstants.Margin + 280 + (int)shakeOffset.Y;

        Raylib.DrawText("EVENTS", x, startY - 22, 14, GameColors.HudDim);
        Raylib.DrawLine(x, startY - 5, x + GameConstants.SidePanelWidth - 40, startY - 5, new Color(50, 55, 65, 255));

        var y = startY;
        var messages = messageLog.TakeLast(6).ToArray();
        
        for (var i = 0; i < messages.Length; i++)
        {
            var message = messages[i];
            var alpha = 150 + i * 17;
            var color = GetMessageColor(message.Kind, alpha);
            Raylib.DrawText($"• {message.Text}", x, y, 14, color);
            y += 18;
        }
    }

    private static Color GetMessageColor(GameMessageKind kind, int alpha) => kind switch
    {
        GameMessageKind.Success => GameColors.WithAlpha(GameColors.Success, alpha),
        GameMessageKind.Damage => GameColors.WithAlpha(GameColors.Shot, alpha),
        GameMessageKind.Warning => new Color( (byte)255,  (byte)200,  (byte)100, (byte)alpha),
        GameMessageKind.Bonus => GameColors.WithAlpha(GameColors.Gold, alpha),
        GameMessageKind.PowerUp => GameColors.WithAlpha(GameColors.Cyan, alpha),
        _ => GameColors.WithAlpha(GameColors.HudText, alpha)
    };

    public void DrawInGameHints(int gridWidth, int gridHeight, Vector2 shakeOffset)
    {
        var x = GameConstants.Margin * 2 + gridWidth + (int)shakeOffset.X + 12;
        var y = GameConstants.Margin + Math.Max(gridHeight, 400) - 90;

        Raylib.DrawRectangle(x - 5, y, GameConstants.SidePanelWidth - 24, 80, new Color(32, 35, 42, 255));
        Raylib.DrawRectangleLinesEx(new Rectangle(x - 5, y, GameConstants.SidePanelWidth - 24, 80), 1, new Color(45, 48, 58, 255));

        Raylib.DrawText("CONTROLS", x, y + 8, 13, GameColors.HudDim);
        Raylib.DrawText("WASD / Arrows - Move", x, y + 23, 14, GameColors.HudDim);
        Raylib.DrawText("ESC - Menu  |  Q - Quit", x, y + 38, 14, GameColors.HudDim);
        Raylib.DrawText("Find the gem to win!", x, y + 58, 14, GameColors.Cyan);
    }

    #endregion

    #region Overlays

    public void DrawChapterCompleteOverlay(RealTimeGameSession session, float fadeIn, float scale)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        // Background
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(15, 35, 25, (int)(235 * fadeIn)));

        var centerX = screenW / 2;
        var centerY = screenH / 2 - 40;
        var panelW = (int)(420 * scale);
        var panelH = (int)(340 * scale);

        // Panel
        Raylib.DrawRectangle(centerX - panelW / 2 + 5, centerY - panelH / 2 + 5, panelW, panelH, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH, new Color(25, 45, 35, 250));
        Raylib.DrawRectangleLinesEx(new Rectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH), 3, GameColors.Success);

        if (scale > 0.5f)
        {
            DrawSpriteAtCenter(_sprites.Trophy, centerX, centerY - panelH / 2 + 45, 56);

            var title = "CHAPTER COMPLETE!";
            Raylib.DrawText(title, centerX - Raylib.MeasureText(title, 32) / 2, centerY - panelH / 2 + 85, 32, GameColors.Success);

            var statsY = centerY - panelH / 2 + 135;
            DrawStatLine(centerX, statsY, "Chapter", $"{session.ChapterNumber}", GameColors.Gold); statsY += 30;
            DrawStatLine(centerX, statsY, "Score", $"{session.Rat.Score:N0}", GameColors.Gold); statsY += 30;
            DrawStatLine(centerX, statsY, "Health", $"{session.Rat.Health}/{session.Rat.MaxHealth}", 
                session.Rat.Health == session.Rat.MaxHealth ? GameColors.Success : GameColors.HudText); statsY += 30;
            DrawStatLine(centerX, statsY, "Combo", $"x{session.Rat.ComboCount}", 
                session.Rat.ComboCount >= 10 ? GameColors.Gold : GameColors.HudText);

            DrawButton(GetCenterButtonRect(0), "NEXT CHAPTER", GameColors.ButtonSuccess, GameColors.ButtonSuccessHover);
        }
    }

    public void DrawGameOverOverlay(RealTimeGameSession session, float fadeIn, float scale)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();

        // Background
        Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(40, 15, 15, (int)(235 * fadeIn)));

        var centerX = screenW / 2;
        var centerY = screenH / 2 - 30;
        var panelW = (int)(420 * scale);
        var panelH = (int)(400 * scale);

        // Panel
        Raylib.DrawRectangle(centerX - panelW / 2 + 5, centerY - panelH / 2 + 5, panelW, panelH, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH, new Color(50, 25, 25, 250));
        Raylib.DrawRectangleLinesEx(new Rectangle(centerX - panelW / 2, centerY - panelH / 2, panelW, panelH), 3, GameColors.Shot);

        if (scale > 0.5f)
        {
            DrawSpriteAtCenter(_sprites.Skull, centerX, centerY - panelH / 2 + 45, 56);

            var title = "GAME OVER";
            Raylib.DrawText(title, centerX - Raylib.MeasureText(title, 38) / 2, centerY - panelH / 2 + 85, 38, GameColors.Shot);

            var statsY = centerY - panelH / 2 + 140;
            DrawStatLine(centerX, statsY, "Final Score", $"{session.Rat.Score:N0}", GameColors.Gold); statsY += 28;
            DrawStatLine(centerX, statsY, "Chapters Cleared", $"{session.ChapterNumber - 1}", GameColors.HudText); statsY += 28;
            DrawStatLine(centerX, statsY, "Gems Collected", $"{session.Rat.GemsCollected}", GameColors.Cyan); statsY += 28;
            DrawStatLine(centerX, statsY, "Max Combo", $"x{session.Rat.MaxCombo}", 
                session.Rat.MaxCombo >= 10 ? GameColors.Gold : GameColors.HudText); statsY += 28;
            DrawStatLine(centerX, statsY, "Shots Dodged", $"{session.Rat.ShotsAvoided}", GameColors.Success);

            DrawButton(GetCenterButtonRect(0), "PLAY AGAIN", GameColors.ButtonPrimary, GameColors.ButtonPrimaryHover);
            DrawButton(GetCenterButtonRect(1), "MAIN MENU", GameColors.ButtonDark, GameColors.ButtonDarkHover);
        }
    }

    public void DrawLevelTransition(float alpha)
    {
        if (alpha > 0)
        {
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), 
                GameColors.WithAlpha(GameColors.Background, alpha));
        }
    }

    private void DrawStatLine(int centerX, int y, string label, string value, Color valueColor)
    {
        Raylib.DrawText($"{label}:", centerX - 90, y, 18, GameColors.HudDim);
        Raylib.DrawText(value, centerX + 50, y, 20, valueColor);
    }

    public static Rectangle GetCenterButtonRect(int buttonIndex)
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;
        var centerY = screenH / 2 + 70 + buttonIndex * (GameConstants.ButtonHeight + 15);
        return new Rectangle(centerX - GameConstants.ButtonWidth / 2, centerY, GameConstants.ButtonWidth, GameConstants.ButtonHeight);
    }

    #endregion

    #region Buttons

    public void DrawButton(Rectangle rect, string text, Color normalColor, Color hoverColor)
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

        // Shadow
        Raylib.DrawRectangle((int)scaledRect.X + 3, (int)scaledRect.Y + 3, (int)scaledRect.Width, (int)scaledRect.Height, new Color(0, 0, 0, 60));
        // Button
        Raylib.DrawRectangleRec(scaledRect, color);
        Raylib.DrawRectangleLinesEx(scaledRect, 2, isHovered ? Color.White : new Color(255, 255, 255, 150));

        // Text
        var textW = Raylib.MeasureText(text, 24);
        var textX = (int)(scaledRect.X + scaledRect.Width / 2 - textW / 2);
        var textY = (int)(scaledRect.Y + scaledRect.Height / 2 - 12);
        Raylib.DrawText(text, textX, textY, 24, Color.White);
    }

    public static bool CheckCollisionPointRec(Vector2 point, Rectangle rec)
    {
        return point.X >= rec.X && point.X <= rec.X + rec.Width &&
               point.Y >= rec.Y && point.Y <= rec.Y + rec.Height;
    }

    #endregion

    #region Sprite Helpers

    private void DrawSprite(Texture2D texture, int px, int py, float scale, Color? tint = null)
    {
        var size = GameConstants.TileSize * scale;
        var src = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(px + GameConstants.TileSize / 2f, py + GameConstants.TileSize / 2f, size, size);
        var origin = new Vector2(size / 2f, size / 2f);
        Raylib.DrawTexturePro(texture, src, dest, origin, 0, tint ?? Color.White);
    }

    private static void DrawSpriteAt(Texture2D texture, int x, int y, int size, Color tint)
    {
        var src = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(x + size / 2f, y + size / 2f, size, size);
        var origin = new Vector2(size / 2f, size / 2f);
        Raylib.DrawTexturePro(texture, src, dest, origin, 0, tint);
    }

    private void DrawSpriteAtCenter(Texture2D texture, int cx, int cy, int size)
    {
        var src = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(cx, cy, size, size);
        var origin = new Vector2(size / 2f, size / 2f);
        Raylib.DrawTexturePro(texture, src, dest, origin, 0, Color.White);
    }

    #endregion
}
