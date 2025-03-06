using System.Collections.Generic;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Items.Shims;

public class PuzzleItemShim : ItemShim
{
    public int PuzzleIndex { get; set; }
    public int MapId { get; set; }
    public List<AssociatedPuzzleTopic> AssociatedTopics { get; set; }
    public Speaker AccompanyingCharacter { get; set; }
    public Speaker PowerCharacter1 { get; set; }
    public Speaker PowerCharacter2 { get; set; }

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
    }
}
