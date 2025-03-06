namespace SerialLoops.Lib.Items.Shims;

public class ChessPuzzleItemShim : ItemShim
{
    public int Index { get; set; }

    public ChessPuzzleItemShim()
    {
    }

    public ChessPuzzleItemShim(ChessPuzzleItem chessPuzzle) : base(chessPuzzle)
    {
        Index = chessPuzzle.ChessPuzzle.Index;
    }
}
