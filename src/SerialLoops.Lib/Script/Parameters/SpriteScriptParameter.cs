using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class SpriteScriptParameter : ScriptParameter
{
    public CharacterSpriteItemShim Sprite { get; set; }
    public override short[] GetValues(object obj = null) => [(short)(Sprite?.Index ?? 0)];
    public CharacterSpriteItem GetSprite(ILiteCollection<ItemDescription> itemsCol) => (CharacterSpriteItem)Sprite?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Sprite?.DisplayName;
    }

    public SpriteScriptParameter(string name, CharacterSpriteItemShim sprite) : base(name, ParameterType.SPRITE)
    {
        Sprite = sprite;
    }

    public override SpriteScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Sprite);
    }
}
