using System.Collections.Generic;
using System.Linq;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using SerialLoops.Lib.Script.Parameters;

namespace SerialLoops.Lib.Items.Shims;

public class PuzzleItemShim : ItemShim
{
    public int PuzzleIndex { get; set; }
    public int MapId { get; set; }
    public List<AssociatedPuzzleTopic> AssociatedTopics { get; set; }
    public Speaker AccompanyingCharacter { get; set; }
    public Speaker PowerCharacter1 { get; set; }
    public Speaker PowerCharacter2 { get; set; }

    public int[] Flags { get; set; }
    public string FlagNames { get; set; }

    public PuzzleItemShim()
    {
    }

    public PuzzleItemShim(PuzzleItem puzzle) : base(puzzle)
    {
        PuzzleIndex = puzzle.Puzzle.Index;
        MapId = puzzle.Puzzle.Settings.MapId;
        AssociatedTopics = puzzle.Puzzle.AssociatedTopics;
        AccompanyingCharacter = puzzle.Puzzle.Settings.AccompanyingCharacter;
        PowerCharacter1 = puzzle.Puzzle.Settings.PowerCharacter1;
        PowerCharacter2 = puzzle.Puzzle.Settings.PowerCharacter2;
        Flags = [puzzle.Puzzle.Settings.Unknown15, puzzle.Puzzle.Settings.Unknown16];
        FlagNames = string.Join(' ', Flags.Select(f => new FlagScriptParameter("Flag", (short)f).FlagName));
    }
}
