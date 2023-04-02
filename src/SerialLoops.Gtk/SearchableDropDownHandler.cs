using Eto.Forms;
using Eto.GtkSharp.Forms.Controls;
using RealGtk = Gtk;
using System.Text.RegularExpressions;
using System.Timers;

namespace SerialLoops.Gtk
{
    public class SearchableDropDownHandler : DropDownHandler<RealGtk.ComboBox, DropDown, DropDown.ICallback>
    {
        private string _currentSearchedText = string.Empty;
        private Timer _searchedTextTimer;

        protected override void Create()
        {
            Control = new RealGtk.ComboBox();
            var imageCell = new RealGtk.CellRendererPixbuf();
            Control.PackStart(imageCell, false);
            Control.AddAttribute(imageCell, "pixbuf", 1);
            text = new RealGtk.CellRendererText();

            Control.PackStart(text, true);
            Control.KeyPressEvent += Control_KeyPressEvent;
            Control.Changed += Connector.HandleChanged;

            _searchedTextTimer = new(1000);
            _searchedTextTimer.Elapsed += SearchedTextTimer_Elapsed;
        }

        private void Control_KeyPressEvent(object o, RealGtk.KeyPressEventArgs args)
        {
            _searchedTextTimer.Stop();
            _searchedTextTimer.Start();

            if (Regex.IsMatch(args.Event.Key.ToString(), @"^[A-Z]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                _currentSearchedText += args.Event.Key.ToString();
            }
        }

        private void SearchedTextTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
