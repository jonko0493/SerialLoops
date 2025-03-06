using System.Threading.Tasks;
using System.Windows.Input;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using SerialLoops.Assets;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Lib.SaveFile;
using SerialLoops.ViewModels.Controls;
using SerialLoops.ViewModels.Dialogs;
using SerialLoops.ViewModels.Panels;
using SerialLoops.Views.Dialogs;

namespace SerialLoops.ViewModels.Editors;

public class SaveEditorViewModel : EditorViewModel
{
    public SaveItem Save { get; }
    public SaveSlotPreviewViewModel Slot1ViewModel => new(Description, Save, Save.Save.CheckpointSaveSlots[0], 1, Window);
    public SaveSlotPreviewViewModel Slot2ViewModel => new(Description, Save, Save.Save.CheckpointSaveSlots[1], 2, Window);
    public SaveSlotPreviewViewModel QuickSaveViewModel => new(Description, Save, Save.Save.QuickSaveSlot, 3, Window);

    public ICommand EditCommonSaveDataCommand { get; }

    public SaveEditorViewModel(ReactiveItemDescription item, MainWindowViewModel window, ILogger log, EditorTabsPanelViewModel tabs = null) :
        base(item, window, log, tabs: tabs)
    {
        Save = (SaveItem)item.Item;

        EditCommonSaveDataCommand = ReactiveCommand.CreateFromTask(EditCommonData);
    }

    private async Task EditCommonData()
    {
        await new SaveSlotEditorDialog()
        {
            DataContext = new SaveSlotEditorDialogViewModel(Description, Save, Save.Save.CommonData, Save.DisplayName,
                Strings.Common_Save_Data, Window.OpenProject, _log, _tabs),
        }.ShowDialog(Window.Window);
    }
}
