using Raylib_cs;

namespace Rat.Desktop;

internal static class SpriteArt
{
    public static SpriteDefinition RatA { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            "......ee........",
            ".....eBBBe......",
            "....eBBBBBe.....",
            "...eBBbBBbBe....",
            "..eBBBBBBBBBe...",
            "..eBBBkBBBkBe...",
            "..eBBBBBBBBBe...",
            "...eBBBBBBBe....",
            "....eBBBBBe.....",
            ".....eBBBe......",
            "......ee..ttt...",
            "...........t....",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['e'] = new Color(238, 172, 170, 255), // ear pink
            ['B'] = new Color(196, 140, 82, 255),  // body light
            ['b'] = new Color(168, 112, 60, 255),  // body shade
            ['k'] = new Color(24, 24, 28, 255),    // eyes
            ['t'] = new Color(225, 170, 164, 255), // tail
        });

    public static SpriteDefinition RatB { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            "......ee........",
            ".....eBBBe......",
            "....eBBBBBe.....",
            "...eBBbBBbBe....",
            "..eBBBBBBBBBe...",
            "..eBBBkBBBkBe...",
            "..eBBBBBBBBBe...",
            "...eBBBBBBBe....",
            "....eBBBBBe.....",
            ".....eBBBe......",
            "...ttt..ee......",
            "....t...........",
            "................",
            "................",
        },
        Palette: RatA.Palette);

    public static SpriteDefinition Rock { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            "......oooo......",
            "....ooOOOOoo....",
            "...oOOOHHOOOo...",
            "..oOOHHHHHOOo...",
            "..oOOHHHhHOOo...",
            "..oOOHHhhhOOo...",
            "..oOOHHhhhOOo...",
            "..oOOHHHhHOOo...",
            "..oOOHHHHHOOo...",
            "...oOOOHHOOOo...",
            "....ooOOOOoo....",
            "......oooo......",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['o'] = new Color(40, 40, 52, 255),    // outline
            ['O'] = new Color(126, 126, 140, 255), // base
            ['H'] = new Color(170, 170, 186, 255), // highlight
            ['h'] = new Color(104, 104, 118, 255), // shadow
        });

    public static SpriteDefinition SnakeA { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            ".....gggg.......",
            "...ggGGGGgg.....",
            "..gGGGggGGGg....",
            "..gGGg..gGGg....",
            "...gGGggGGg.....",
            "....gGGGGg......",
            ".....gGGg.......",
            "....gGGGGg......",
            "...gGGggGGg.....",
            "..gGGg..gGGg....",
            "..gGGGggGGGg....",
            "...ggGGGGgg.....",
            ".....gggg.......",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['g'] = new Color(34, 70, 46, 255),    // outline
            ['G'] = new Color(66, 196, 98, 255),   // body
        });

    public static SpriteDefinition SnakeB { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            ".....gggg.......",
            "...ggGGGGgg.....",
            "..gGGGggGGGg....",
            "..gGGg..gGGg....",
            "...gGGggGGg.....",
            "....gGGGGg......",
            ".....gGGg..r....",
            "....gGGGGg.r....",
            "...gGGggGGg.....",
            "..gGGg..gGGg....",
            "..gGGGggGGGg....",
            "...ggGGGGgg.....",
            ".....gggg.......",
            "................",
        },
        Palette: new Dictionary<char, Color>(SnakeA.Palette)
        {
            ['r'] = new Color(226, 70, 70, 255), // tongue
        });

    public static SpriteDefinition GemA { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".......yy.......",
            "......yYYy......",
            ".....yYCCYy.....",
            "....yYCCCCYy....",
            "...yYCCCCCCYy...",
            "..yYCCCCCCCCYy..",
            ".yYCCCCCCCCCCYy.",
            ".yYCCCCCCCCCCYy.",
            "..yYCCCCCCCCYy..",
            "...yYCCCCCCYy...",
            "....yYCCCCYy....",
            ".....yYCCYy.....",
            "......yYYy......",
            ".......yy.......",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['y'] = new Color(96, 78, 24, 255),     // outline
            ['Y'] = new Color(234, 204, 90, 255),   // gold
            ['C'] = new Color(120, 220, 255, 255),  // crystal
        });

    public static SpriteDefinition GemB { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".......yy.......",
            "......yYYy......",
            ".....yYCCYy.....",
            "....yYCCCCYy....",
            "...yYCCCCCCYy...",
            "..yYCCCCCCCCYy..",
            ".yYCCCC*CCCCCYy.",
            ".yYCCCCCC*CCCYy.",
            "..yYCCCCCCCCYy..",
            "...yYCCCCCCYy...",
            "....yYCCCCYy....",
            ".....yYCCYy.....",
            "......yYYy......",
            ".......yy.......",
            "................",
        },
        Palette: new Dictionary<char, Color>(GemA.Palette)
        {
            ['*'] = new Color(255, 255, 255, 255),
        });

    public static SpriteDefinition Crosshair { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "......rrrr......",
            ".....r....r.....",
            "....r..rr..r....",
            "...r..r..r..r...",
            "..r..r....r..r..",
            "..r..r.rr.r..r..",
            "rr..r.r..r.r..rr",
            "rr..r.r..r.r..rr",
            "..r..r.rr.r..r..",
            "..r..r....r..r..",
            "...r..r..r..r...",
            "....r..rr..r....",
            ".....r....r.....",
            "......rrrr......",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['r'] = new Color(225, 70, 70, 255),
        });

    public static SpriteDefinition HeartFull { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "....rr....rr....",
            "...rRRr..rRRr...",
            "..rRRRRrrRRRRr..",
            "..rRRRRRRRRRRr..",
            "..rRRRRRRRRRRr..",
            "...rRRRRRRRRr...",
            "....rRRRRRRr....",
            ".....rRRRRr.....",
            "......rRRr......",
            ".......rr.......",
            "................",
            "................",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['r'] = new Color(120, 22, 22, 255),
            ['R'] = new Color(232, 66, 66, 255),
        });

    public static SpriteDefinition HeartEmpty { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "....rr....rr....",
            "...r..r..r..r...",
            "..r....rr....r..",
            "..r..........r..",
            "..r..........r..",
            "...r........r...",
            "....r......r....",
            ".....r....r.....",
            "......r..r......",
            ".......rr.......",
            "................",
            "................",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['r'] = new Color(140, 72, 72, 255),
        });

    public static SpriteDefinition HealthPickup { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".......gg.......",
            "......gGGg......",
            ".....gGGGGg.....",
            "....gGGrrGGg....",
            "...gGGrrrrGGg...",
            "..gGGrrrRrrGGg..",
            ".gGGrrrRRRrrGGg.",
            ".gGGrrrRRRrrGGg.",
            "..gGGrrrRrrGGg..",
            "...gGGrrrrGGg...",
            "....gGGrrGGg....",
            ".....gGGGGg.....",
            "......gGGg......",
            ".......gg.......",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['g'] = new Color(34, 78, 34, 255),     // outline
            ['G'] = new Color(60, 140, 60, 255),   // green background
            ['r'] = new Color(180, 40, 40, 255),   // cross dark
            ['R'] = new Color(240, 80, 80, 255),   // cross light
        });

    public static SpriteDefinition Shield { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "....bbbbbbbb....",
            "...bBBBBBBBBb...",
            "..bBBBBBBBBBBb..",
            "..bBBBccccBBBb..",
            "..bBBBcCCcBBBb..",
            "..bBBBcCCcBBBb..",
            "..bBBBcCCcBBBb..",
            "...bBBcCCcBBb...",
            "...bBBcCCcBBb...",
            "....bBcCCcBb....",
            ".....bcCCcb.....",
            "......bccb......",
            ".......bb.......",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['b'] = new Color(40, 50, 90, 255),    // border
            ['B'] = new Color(70, 130, 200, 255),  // shield blue
            ['c'] = new Color(180, 160, 60, 255),  // crest outline
            ['C'] = new Color(240, 220, 100, 255), // crest gold
        });

    public static SpriteDefinition SpeedBoost { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "..........yy....",
            ".........yYy....",
            "........yYy.....",
            ".......yYy......",
            "......yYYyyyy...",
            ".....yYYYYYYy...",
            "....yYYYYYYy....",
            "...yYYYYYYy.....",
            "...yyyyyYYy.....",
            ".......yYy......",
            "......yYy.......",
            ".....yYy........",
            ".....yy.........",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['y'] = new Color(180, 140, 40, 255),  // outline
            ['Y'] = new Color(255, 230, 80, 255),  // lightning yellow
        });
}

internal sealed record SpriteDefinition(int Width, int Height, string[] Pixels, IReadOnlyDictionary<char, Color> Palette);
