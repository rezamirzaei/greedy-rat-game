using Raylib_cs;

namespace Rat.Desktop;

internal sealed class SpriteTextures : IDisposable
{
    public SpriteTextures()
    {
        RatA = CreateTexture(SpriteArt.RatA);
        RatB = CreateTexture(SpriteArt.RatB);
        Rock = CreateTexture(SpriteArt.Rock);
        SnakeA = CreateTexture(SpriteArt.SnakeA);
        SnakeB = CreateTexture(SpriteArt.SnakeB);
        GemA = CreateTexture(SpriteArt.GemA);
        GemB = CreateTexture(SpriteArt.GemB);
        Crosshair = CreateTexture(SpriteArt.Crosshair);
        HeartFull = CreateTexture(SpriteArt.HeartFull);
        HeartEmpty = CreateTexture(SpriteArt.HeartEmpty);
        HealthPickup = CreateTexture(SpriteArt.HealthPickup);
        Shield = CreateTexture(SpriteArt.Shield);
        SpeedBoost = CreateTexture(SpriteArt.SpeedBoost);
        Star = CreateTexture(SpriteArt.Star);
        Trophy = CreateTexture(SpriteArt.Trophy);
        Skull = CreateTexture(SpriteArt.Skull);
    }

    public Texture2D RatA { get; }
    public Texture2D RatB { get; }
    public Texture2D Rock { get; }
    public Texture2D SnakeA { get; }
    public Texture2D SnakeB { get; }
    public Texture2D GemA { get; }
    public Texture2D GemB { get; }
    public Texture2D Crosshair { get; }
    public Texture2D HeartFull { get; }
    public Texture2D HeartEmpty { get; }
    public Texture2D HealthPickup { get; }
    public Texture2D Shield { get; }
    public Texture2D SpeedBoost { get; }
    public Texture2D Star { get; }
    public Texture2D Trophy { get; }
    public Texture2D Skull { get; }

    public void Dispose()
    {
        Raylib.UnloadTexture(RatA);
        Raylib.UnloadTexture(RatB);
        Raylib.UnloadTexture(Rock);
        Raylib.UnloadTexture(SnakeA);
        Raylib.UnloadTexture(SnakeB);
        Raylib.UnloadTexture(GemA);
        Raylib.UnloadTexture(GemB);
        Raylib.UnloadTexture(Crosshair);
        Raylib.UnloadTexture(HeartFull);
        Raylib.UnloadTexture(HeartEmpty);
        Raylib.UnloadTexture(HealthPickup);
        Raylib.UnloadTexture(Shield);
        Raylib.UnloadTexture(SpeedBoost);
        Raylib.UnloadTexture(Star);
        Raylib.UnloadTexture(Trophy);
        Raylib.UnloadTexture(Skull);
    }

    private static Texture2D CreateTexture(SpriteDefinition definition)
    {
        var image = Raylib.GenImageColor(definition.Width, definition.Height, new Color(0, 0, 0, 0));

        for (var y = 0; y < definition.Height; y++)
        {
            var row = definition.Pixels[y];
            for (var x = 0; x < definition.Width; x++)
            {
                var ch = row[x];
                if (ch == '.')
                    continue;

                if (!definition.Palette.TryGetValue(ch, out var color))
                    color = new Color(255, 0, 255, 255); // magenta = missing palette entry

                Raylib.ImageDrawPixel(ref image, x, y, color);
            }
        }

        var texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);

        Raylib.SetTextureFilter(texture, TextureFilter.Point);
        return texture;
    }
}

