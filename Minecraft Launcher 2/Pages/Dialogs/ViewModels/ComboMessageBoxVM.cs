using System.Collections.Generic;

namespace Minecraft_Launcher_2.Pages.Dialogs.ViewModels
{
    public class ComboMessageBoxVM : ObservableObject
    {
        public ComboMessageBoxVM(string message, IEnumerable<object> items)
        {
            Message = message;
            Items = items;
        }

        public string Message { get; }

        public IEnumerable<object> Items { get; }

        public int SelectedIndex { get; set; } = 0;
    }
}