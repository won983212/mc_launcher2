using System.Windows.Input;

namespace Minecraft_Launcher_2.Pages.ViewModels.ServerSetting
{
    internal class GeneralTabVM : TabChild<ServerSettingPanelVM>
    {
        public GeneralTabVM(ServerSettingPanelVM parent)
            : base(parent)
        { }

        public ICommand UpdateVersionToDateCommand => new RelayCommand(Parent.UpdateVersionToToday);

        public ICommand ChangeAPIServerDirectoryCommand => new RelayCommand(() => Parent.ResetAPIServerDirectory());
    }
}
