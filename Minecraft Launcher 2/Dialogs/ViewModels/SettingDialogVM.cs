using System.Windows.Input;

namespace Minecraft_Launcher_2.Dialogs.ViewModels
{
    public class SettingDialogVM : ObservableObject
    {
        public ICommand SaveCommand => new RelayCommand(() =>
        {
            Properties.Settings.Default.Save();
            CommonUtils.CloseDialog();
        });

        public ICommand ResetCommand => new RelayCommand(Properties.Settings.Default.Reset);
    }
}
