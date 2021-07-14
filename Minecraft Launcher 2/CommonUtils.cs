using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    public class CommonUtils
    {
        public delegate void DialogCompleteEventHandler<T>(T vm, DialogClosingEventArgs eventArgs) where T : ObservableObject;


        public static bool IsLegalUsername(string name)
        {
            foreach (char c in name)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".IndexOf(c) == -1)
                    return false;
            }

            return true;
        }

        #region Dialog

        public static Task<object> ShowDialog<T>(T content, DialogCompleteEventHandler<T> closingHandler = null) where T : ObservableObject
        {
            return ShowDialog(content, "RootDialogHost", closingHandler);
        }

        public static Task<object> ShowDialog<T>(T content, string dialog, DialogCompleteEventHandler<T> closingHandler = null) where T : ObservableObject
        {
            return DialogHost.Show(content, dialog, (o, e) => closingHandler?.Invoke((T)((DialogHost)o).DialogContent, e));
        }

        public static void CloseDialog()
        {
            DialogHost.Close("RootDialogHost");
        }

        #endregion
    }
}
