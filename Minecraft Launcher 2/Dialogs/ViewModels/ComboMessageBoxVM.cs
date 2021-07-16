using System.Collections.Generic;

namespace Minecraft_Launcher_2.Dialogs.ViewModels
{
    public class ComboMessageBoxVM : ObservableObject
    {
        public ComboMessageBoxVM(string message, IEnumerable<object> items)
        {
            Message = message;
            Items = items;
        }

        public string Message { get; private set; }

        public IEnumerable<object> Items { get; private set; }

        public int SelectedIndex { get; set; } = 0;
    }
}
