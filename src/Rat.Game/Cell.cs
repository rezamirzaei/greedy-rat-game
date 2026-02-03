namespace Rat.Game;

public sealed class Cell
{
    public Cell(CellContent content)
    {
        Content = content;
    }

    public bool IsDug { get; set; }
    public CellContent Content { get; set; }
}

