namespace SerialLoops.Lib.Items.Shims;

public class PlaceItemShim : ItemShim
{
    public int Index { get; set; }
    public int GraphicIndex { get; set; }

    public PlaceItemShim()
    {
    }

    public PlaceItemShim(PlaceItem place) : base(place)
    {
        Index = place.Index;
        GraphicIndex = place.PlaceGraphic.Index;
    }
}
