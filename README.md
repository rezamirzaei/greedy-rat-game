# RAT (2D, chapter-based) — C# / OOP

You play as a rat exploring a dusty 2D grid. Each chapter gets harder:

- **Goal:** find the hidden **gem**.
- **Dust blocks:** most tiles start as **dust**; when you try to move into a tile, the dust is removed and the tile is revealed.
- **Surprises:** a revealed tile might be **empty**, a **rock** (blocks movement), or a **snake** (hurts you).
- **Shooter:** incoming shots are shown as **targets (X)** and land when the **countdown** reaches 0.

## Run

### Rider / Visual Studio
- Open `Solution1.sln`
- Run the `Rat.Desktop` project (recommended)

### CLI
Requires the **.NET 8 SDK**:

If `dotnet` is not on your PATH, use `~/.dotnet/dotnet` instead.

```bash
dotnet run --project src/Rat.Desktop
```

Optional deterministic run (same levels every time):

```bash
dotnet run --project src/Rat.Desktop -- --seed 123
```

Console version (simple fallback):

```bash
dotnet run --project src/Rat.Cli
```

## Controls

- Move: **WASD** or **Arrow keys**
- Next chapter: **N** / **Enter**
- Restart (after game over): **R**
- Quit: **Q** / **Esc**
