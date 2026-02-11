using Raylib_cs;

namespace Rat.Desktop;

internal static class SpriteArt
{
    // Cute, expressive rat character
    public static SpriteDefinition RatA { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "....pp....pp....",
            "...pPPp..pPPp...",
            "...pPPPppPPPp...",
            "..pPPPPPPPPPPp..",
            "..pPPPPPPPPPPp..",
            "..pPPowPPwoPPp..",
            "..pPPPPnPPPPPp..",
            "..pPPPPPPPPPPp..",
            "...pPPPPPPPPp...",
            "....pPPPPPPp....",
            ".....pppppp.....",
            "......pppp..tt..",
            "............t...",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['p'] = new Color(90, 70, 50, 255),    // outline brown
            ['P'] = new Color(200, 160, 120, 255), // body fur
            ['o'] = new Color(255, 255, 255, 255), // eye white
            ['w'] = new Color(20, 20, 30, 255),    // eye pupil
            ['n'] = new Color(255, 150, 150, 255), // nose pink
            ['t'] = new Color(255, 180, 170, 255), // tail
        });

    public static SpriteDefinition RatB { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "....pp....pp....",
            "...pPPp..pPPp...",
            "...pPPPppPPPp...",
            "..pPPPPPPPPPPp..",
            "..pPPPPPPPPPPp..",
            "..pPPowPPwoPPp..",
            "..pPPPPnPPPPPp..",
            "..pPPPPPPPPPPp..",
            "...pPPPPPPPPp...",
            "....pPPPPPPp....",
            ".....pppppp.....",
            "..tt..pppp......",
            "...t............",
            "................",
            "................",
        },
        Palette: RatA.Palette);

    // Solid, detailed rock
    public static SpriteDefinition Rock { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".....oooooo.....",
            "....oHHHHHHo....",
            "...oHHHHhhHHo...",
            "..oHHHHhhhhHHo..",
            "..oHHhhhhhhhHo..",
            "..oHhhhhsshHHo..",
            "..oHhhhsssshHo..",
            "..oHhhhsssshHo..",
            "..oHhhhhsshHHo..",
            "..oHHhhhhhhhHo..",
            "...oHHhhhhHHo...",
            "....oHHHHHHo....",
            ".....oooooo.....",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['o'] = new Color(50, 50, 60, 255),    // dark outline
            ['H'] = new Color(140, 140, 155, 255), // highlight
            ['h'] = new Color(100, 100, 115, 255), // mid tone
            ['s'] = new Color(70, 70, 85, 255),    // shadow
        });

    // Menacing but clear snake
    public static SpriteDefinition SnakeA { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            "....dddd........",
            "...dGGGGd.......",
            "..dGgGGgGd......",
            "..dGeGGeGd......",
            "...dGGGGd.......",
            "....dGGd........",
            ".....dGGd.......",
            "......dGGd......",
            ".......dGGd.....",
            "........dGGGd...",
            ".........dGGd...",
            "..........dd....",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['d'] = new Color(30, 80, 40, 255),    // dark outline
            ['G'] = new Color(80, 200, 100, 255),  // body green
            ['g'] = new Color(60, 160, 80, 255),   // body pattern
            ['e'] = new Color(255, 50, 50, 255),   // eyes red
        });

    public static SpriteDefinition SnakeB { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "................",
            "....dddd........",
            "...dGGGGd.......",
            "..dGgGGgGd......",
            "..dGeGGeGdrr....",
            "...dGGGGdrr.....",
            "....dGGd........",
            ".....dGGd.......",
            "......dGGd......",
            ".......dGGd.....",
            "........dGGGd...",
            ".........dGGd...",
            "..........dd....",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>(SnakeA.Palette)
        {
            ['r'] = new Color(220, 50, 80, 255), // tongue
        });

    // Beautiful, shimmering gem - the goal!
    public static SpriteDefinition GemA { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".......gg.......",
            "......gGGg......",
            ".....gGCCGg.....",
            "....gGCcCCGg....",
            "...gGCcccCCGg...",
            "..gGCccWccCCGg..",
            ".gGCcccWWcccCGg.",
            ".gGCccccccccCGg.",
            "..gGCccccccCGg..",
            "...gGCccccCGg...",
            "....gGCccCGg....",
            ".....gGCCGg.....",
            "......gGGg......",
            ".......gg.......",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['g'] = new Color(30, 100, 130, 255),  // outline
            ['G'] = new Color(60, 180, 220, 255),  // outer glow
            ['C'] = new Color(100, 220, 255, 255), // crystal
            ['c'] = new Color(150, 240, 255, 255), // crystal light
            ['W'] = new Color(255, 255, 255, 255), // sparkle
        });

    public static SpriteDefinition GemB { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".......gg.......",
            "......gGGg......",
            ".....gGCCGg.....",
            "....gGCcCCGg....",
            "...gGCcccCCGg...",
            "..gGCccccWCCGg..",
            ".gGCcccccWWcCGg.",
            ".gGCccccccccCGg.",
            "..gGCccWcccCGg..",
            "...gGCccccCGg...",
            "....gGCccCGg....",
            ".....gGCCGg.....",
            "......gGGg......",
            ".......gg.......",
            "................",
        },
        Palette: GemA.Palette);

    // Clear targeting crosshair
    public static SpriteDefinition Crosshair { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            ".......rr.......",
            ".......rr.......",
            ".......rr.......",
            ".......rr.......",
            "....r..rr..r....",
            ".....r.rr.r.....",
            "......rrrr......",
            "rrrrrrrRRrrrrrrr",
            "rrrrrrrRRrrrrrrr",
            "......rrrr......",
            ".....r.rr.r.....",
            "....r..rr..r....",
            ".......rr.......",
            ".......rr.......",
            ".......rr.......",
            ".......rr.......",
        },
        Palette: new Dictionary<char, Color>
        {
            ['r'] = new Color(255, 80, 80, 200),
            ['R'] = new Color(255, 40, 40, 255),
        });

    // Full heart - health indicator
    public static SpriteDefinition HeartFull { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "..ddd....ddd....",
            ".dRRRd..dRRRd...",
            "dRrRRRddRRRrRd..",
            "dRRRRRRRRRRRRd..",
            "dRRRRRRRRRRRRd..",
            ".dRRRRRRRRRRd...",
            "..dRRRRRRRRd....",
            "...dRRRRRRd.....",
            "....dRRRRd......",
            ".....dRRd.......",
            "......dd........",
            "................",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['d'] = new Color(100, 20, 30, 255),
            ['R'] = new Color(220, 50, 70, 255),
            ['r'] = new Color(255, 120, 140, 255), // shine
        });

    public static SpriteDefinition HeartEmpty { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "..ddd....ddd....",
            ".d...d..d...d...",
            "d.....dd.....d..",
            "d............d..",
            "d............d..",
            ".d..........d...",
            "..d........d....",
            "...d......d.....",
            "....d....d......",
            ".....d..d.......",
            "......dd........",
            "................",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['d'] = new Color(120, 80, 90, 255),
        });

    // Health pickup - green cross
    public static SpriteDefinition HealthPickup { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "......oooo......",
            ".....oGGGGo.....",
            "....oGGggGGo....",
            "...oGGgrrrgGo...",
            "..oGGgrrRrrGGo..",
            ".oGGgrRRRRrgGGo.",
            ".oGGgrrRRrrgGGo.",
            ".oGGgrrRRrrgGGo.",
            ".oGGgrRRRRrgGGo.",
            "..oGGgrrRrrGGo..",
            "...oGGgrrrgGo...",
            "....oGGggGGo....",
            ".....oGGGGo.....",
            "......oooo......",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['o'] = new Color(30, 70, 40, 255),    // outline
            ['G'] = new Color(60, 160, 80, 255),   // green bg
            ['g'] = new Color(80, 180, 100, 255),  // green light
            ['r'] = new Color(200, 50, 60, 255),   // cross
            ['R'] = new Color(255, 100, 110, 255), // cross light
        });

    // Shield power-up
    public static SpriteDefinition Shield { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "...oooooooo.....",
            "..oBBBBBBBBo....",
            "..oBBBbbbBBo....",
            "..oBBbsGGbBo....",
            "..oBBbGGGGbBo...",
            "..oBBbGGGGbBo...",
            "...oBbGGGGbBo...",
            "...oBbGGGGbBo...",
            "....obGGGGbo....",
            ".....obGGbo.....",
            "......obbo......",
            ".......oo.......",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['o'] = new Color(30, 50, 90, 255),    // outline
            ['B'] = new Color(70, 140, 220, 255),  // blue
            ['b'] = new Color(100, 170, 240, 255), // blue light
            ['s'] = new Color(180, 150, 50, 255),  // gold shadow
            ['G'] = new Color(255, 220, 80, 255),  // gold emblem
        });

    // Speed boost - lightning bolt
    public static SpriteDefinition SpeedBoost { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".........oo.....",
            "........oYo.....",
            ".......oYYo.....",
            "......oYYYo.....",
            ".....oYYYYo.....",
            "....oYYYYYooo...",
            "...oYYYYYYYYo...",
            "...oooooYYYo....",
            "......oYYYo.....",
            ".....oYYYo......",
            "....oYYYo.......",
            "...oYYo.........",
            "...oYo..........",
            "...oo...........",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['o'] = new Color(150, 120, 30, 255),  // outline
            ['Y'] = new Color(255, 230, 80, 255),  // yellow
        });

    // Star for celebration effects
    public static SpriteDefinition Star { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".......YY.......",
            ".......YY.......",
            "......YYYY......",
            ".....YYYYYY.....",
            "YYYYYYYYYYYYYYYY",
            ".YYYYYYYYYYYYYY.",
            "..YYYYYYYYYYYY..",
            "...YYYYYYYYYY...",
            "...YYYYYYYY.....",
            "...YYY..YYYY....",
            "..YYY....YYYY...",
            "..YY......YYY...",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['Y'] = new Color(255, 230, 80, 255),
        });

    // Trophy for win screen
    public static SpriteDefinition Trophy { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            "GG..........GG..",
            "GGggggggggggGG..",
            "GGggGGGGGGggGG..",
            "GGggGGGGGGggGG..",
            ".GggGGGGGGggG...",
            "..gGGGGGGGGg....",
            "...gGGGGGGg.....",
            "....gGGGGg......",
            ".....gggg.......",
            "......gg........",
            ".....gggg.......",
            "....gggggg......",
            "....gggggg......",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['g'] = new Color(180, 140, 40, 255),  // dark gold
            ['G'] = new Color(255, 220, 80, 255),  // gold
        });

    // Skull for game over
    public static SpriteDefinition Skull { get; } = new(
        Width: 16,
        Height: 16,
        Pixels: new[]
        {
            "................",
            ".....oooooo.....",
            "...ooWWWWWWoo...",
            "..oWWWWWWWWWWo..",
            "..oWWkWWWWkWWo..",
            "..oWWkkWWkkWWo..",
            "..oWWWWkkWWWWo..",
            "..oWWWWWWWWWWo..",
            "...oWWkkkWWo....",
            "...oWkWkWkWo....",
            "....oWWWWWo.....",
            ".....ooooo......",
            "................",
            "................",
            "................",
            "................",
        },
        Palette: new Dictionary<char, Color>
        {
            ['o'] = new Color(80, 80, 90, 255),    // outline
            ['W'] = new Color(230, 230, 235, 255), // white bone
            ['k'] = new Color(30, 30, 35, 255),    // black
        });
}

internal sealed record SpriteDefinition(int Width, int Height, string[] Pixels, IReadOnlyDictionary<char, Color> Palette);
