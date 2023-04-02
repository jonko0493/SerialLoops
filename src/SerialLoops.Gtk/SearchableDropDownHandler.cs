using Eto.Forms;
using Eto.GtkSharp.Forms.Controls;
using Gtk;

namespace CustomGtk
{
    public class SearchableDropDownHandler : DropDownHandler<Gtk.ComboBox, DropDown, DropDown.ICallback>
    {
        protected override void Create()
        {
            Control = new ComboBoxWithEntry();
            var imageCell = new CellRendererPixbuf();
            Control.PackStart(imageCell, false);
            Control.AddAttribute(imageCell, "pixbuf", 1);
            text = new CellRendererText();

            Control.PackStart(text, true);
            Control.Changed += Connector.HandleChanged;
        }
    }

    public class ComboBoxWithEntry : Gtk.ComboBox
    {
        public ComboBoxWithEntry() : base(true)
        {
        }
    }
}
