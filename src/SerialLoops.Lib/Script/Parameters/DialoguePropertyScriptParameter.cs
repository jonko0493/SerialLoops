using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;

namespace SerialLoops.Lib.Script.Parameters;

public class DialoguePropertyScriptParameter : ScriptParameter
{
    public CharacterItemShim Character { get; set; }
    public override short[] GetValues(object obj = null) => [(short)((MessageInfoFile)obj).MessageInfos.FindIndex(m => m.Character == Character.Character),
    ];

    public CharacterItem GetCharacter(ILiteCollection<ItemDescription> itemsCol) => (CharacterItem)Character?.GetItem(itemsCol);

    public override string GetValueString(Project project)
    {
        return Character.DisplayName;
    }

    public DialoguePropertyScriptParameter(string name, CharacterItemShim character) : base(name, ParameterType.CHARACTER)
    {
        Character = character;
    }

    public override DialoguePropertyScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, Character);
    }
}
