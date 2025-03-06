namespace SerialLoops.Lib.Items.Shims;

public class MapItemShim : ItemShim
{
    public int QmapIndex { get; set; }
    public int MapIndex { get; set; }

    public MapItemShim()
    {
    }

    public MapItemShim(MapItem map) : base(map)
    {
        QmapIndex = map.QmapIndex;
        MapIndex = map.Map.Index;
    }
}
