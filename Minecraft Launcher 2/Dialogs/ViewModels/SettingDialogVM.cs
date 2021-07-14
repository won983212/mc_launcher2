using Minecraft_Launcher_2.Updater;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.Dialogs.ViewModels
{
    public class SettingDialogVM : ObservableObject
    {
        private readonly IForceUpdateContoller controller;

        public SettingDialogVM(IForceUpdateContoller controller)
        {
            this.controller = controller;
        }

        private void MarkAsForceUpdate()
        {
            if (controller != null)
            {
                controller.SetForceUpdate();
                MessageBox.Show("강제 업데이트 모드가 설정되었습니다. 설정창에서 나가서 업데이트를 누르면 업데이트가 진행됩니다.", "강제 업데이트 설정됨.");
            }
        }


        public ICommand SaveCommand => new RelayCommand(() =>
        {
            Properties.Settings.Default.Save();
            CommonUtils.CloseDialog();
        });

        public ICommand ResetCommand => new RelayCommand(Properties.Settings.Default.Reset);

        public ICommand MarkAsForceUpdateCommand => new RelayCommand(MarkAsForceUpdate, (o) => controller != null);
    }
}
