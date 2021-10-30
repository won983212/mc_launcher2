using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.ServerConnections;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.Pages.ViewModels.Dialogs
{
    public class SettingDialogVM : ObservableObject
    {
        private string _memorySizeAlertText;

        public SettingDialogVM()
        {
            MaxMemory = CommonUtils.GetTotalMemorySizeGB();
            UpdateMemoryAlertText();
        }


        public string MemorySizeAlertText
        {
            get => _memorySizeAlertText;
            set => SetProperty(ref _memorySizeAlertText, value);
        }

        public int MemorySize
        {
            get => Settings.Default.MemorySize;
            set
            {
                Settings.Default.MemorySize = value;
                OnPropertyChanged();
                UpdateMemoryAlertText();
            }
        }

        public int MaxMemory { get; }

        public bool UseForceUpdate { get; set; }

        public ICommand SaveCommand => new RelayCommand(() =>
        {
            Settings.Default.Save();
            CommonUtils.CloseDialog();
        });

        public ICommand ResetCommand => new RelayCommand(Settings.Default.Reset);

        public ICommand SetForceUpdate => new RelayCommand(() =>
        {
            UseForceUpdate = true;
            MessageBox.Show("강제 업데이트가 적용되었습니다. 이제 설정창을 나가서 업데이트를 누르면 됩니다. ", "안내"
                , MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand SetAPIServerDirectory => new RelayCommand(() =>
        {
            if(CommonUtils.ResetAPIServerDirectory() == null)
            {
                Settings.Default.APIServerDirectory = "";
                Settings.Default.Save();
            }
        });

        public ICommand FindMCDirectory => new RelayCommand(() =>
        {
            var path = CommonUtils.SelectDirectory("마인크래프트 폴더 선택", null, Settings.Default.MinecraftDir);
            if (path != null)
                Settings.Default.MinecraftDir = path;
        });


        private void UpdateMemoryAlertText()
        {
            if (MemorySize > MaxMemory / 2)
                MemorySizeAlertText = "주의! 전체 메모리의 절반이 넘게 할당하면 오히려 성능이 하락할 수 있습니다.";
            else
                MemorySizeAlertText = "";
        }
    }
}