using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class ChessPuzzleScriptParameter : ScriptParameter
{
    public ChessPuzzleItemShim ChessPuzzle { get; set; }
    public override short[] GetValues(object obj = null) => [(short)ChessPuzzle.Index];
    public ChessPuzzleItem GetChessPuzzle(ILiteCollection<ItemDescription> itemsCol) => (ChessPuzzleItem)ChessPuzzle?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return ChessPuzzle.DisplayName;
    }

    public ChessPuzzleScriptParameter(string name, ChessPuzzleItemShim chessPuzzle) : base(name, ParameterType.CHESS_FILE)
    {
        ChessPuzzle =  chessPuzzle;
    }

    public override ChessPuzzleScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, ChessPuzzle);
    }
}
