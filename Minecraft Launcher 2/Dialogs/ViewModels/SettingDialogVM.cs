using Minecraft_Launcher_2.Properties;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.Dialogs.ViewModels
{
    public class SettingDialogVM : ObservableObject
    {
        private string _memorySizeAlertText;

        public SettingDialogVM()
        {
            MaxMemory = CommonUtils.GetTotalMemorySizeGB();
            UpdateMemoryAlertText();
        }


        private void UpdateMemoryAlertText()
        {
            if (MemorySize > MaxMemory / 2)
                MemorySizeAlertText = "주의! 전체 메모리의 절반이 넘게 할당하면 오히려 성능이 하락할 수 있습니다.";
            else
                MemorySizeAlertText = "";
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

        public int MaxMemory { get; private set; }

        public bool UseForceUpdate { get; set; } = false;

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

        public ICommand FindMCDirectory => new RelayCommand(() =>
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "마인크래프트 폴더 선택";
                var result = fbd.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    return;
                Settings.Default.MinecraftDir = fbd.SelectedPath;
            }
        });
    }
}
