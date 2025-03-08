using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using HaruhiChokuretsuLib.Util;
using LiteDB;
using ReactiveUI;
using SerialLoops.Lib;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Lib.Script;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;

namespace SerialLoops.ViewModels.Editors.ScriptCommandEditors;

public partial class PinMnlScriptCommandEditorViewModel : ScriptCommandEditorViewModel
{
    private Project _project;

    public ObservableCollection<CharacterItemShim> Characters { get; }
    private CharacterItemShim _speaker;
    public CharacterItemShim Speaker
    {
        get => _speaker;
        set
        {
            if (value is null)
                return;

            this.RaiseAndSetIfChanged(ref _speaker, value);
            ((DialogueScriptParameter)Command.Parameters[0]).Line.Speaker = _speaker.Character;
            Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Speaker = _speaker.Character;
            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    private string _dialogueLine;
    public string DialogueLine
    {
        get => _dialogueLine.GetSubstitutedString(_project);
        set
        {
            string text = value;
            text = StartStringQuotes().Replace(text, "“");
            text = MidStringOpenQuotes().Replace(text, "$1“");
            text = text.Replace('"', '”');

            this.RaiseAndSetIfChanged(ref _dialogueLine, text.GetOriginalString(_project));

            if (string.IsNullOrEmpty(_dialogueLine))
            {
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Text = "";
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Text = "";
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer = 0;
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Pointer = 0;
            }
            else
            {
                if (((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer == 0)
                {
                    // It doesn't matter what we set this to as long as it's greater than zero
                    // The ASM creation routine only checks that the pointer is not zero
                    ((DialogueScriptParameter)Command.Parameters[0]).Line.Pointer = 1;
                    Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Pointer = 1;
                }
                ((DialogueScriptParameter)Command.Parameters[0]).Line.Text = _dialogueLine;
                Script.Event.DialogueSection.Objects[Command.Section.Objects[Command.Index].Parameters[0]].Text = _dialogueLine;
            }

            ScriptEditor.UpdatePreview();
            ScriptEditor.Description.UnsavedChanges = true;
        }
    }

    public PinMnlScriptCommandEditorViewModel(ScriptItemCommand command, ScriptEditorViewModel scriptEditor, ILogger log, Project project)
        : base(command, scriptEditor, log)
    {
        _project = project;
        LiteDatabase db = new(scriptEditor.Window.OpenProject.DbFile);
        var charCol = db.GetCollection<CharacterItemShim>(nameof(CharacterItem));
        Characters = new(charCol.FindAll());
        _speaker = project.GetCharacterShimBySpeaker(((DialogueScriptParameter)command.Parameters[0]).Line.Speaker, db);
        _dialogueLine = ((DialogueScriptParameter)command.Parameters[0]).Line.Text;
    }

    [GeneratedRegex(@"^""")]
    private static partial Regex StartStringQuotes();
    [GeneratedRegex(@"(\s)""")]
    private static partial Regex MidStringOpenQuotes();
}
