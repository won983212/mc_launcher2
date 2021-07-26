using Minecraft_Launcher_2.Controls;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.Updater;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.Pages.ViewModels.ServerSetting
{
    public class ServerSettingPanelVM : TabContainer
    {
        private bool _isShow;

        private string _version;
        private string _welcomeMessage;


        public ServerSettingPanelVM()
        {
            AddTab(new GeneralTabVM(this));
            AddTab(new UpdateTabVM(this));
            AddTab(new SkinTabVM(this));
            SelectedTabItemIndex = 0;
        }


        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ProgressStatus ProgressData { get; } = new ProgressStatus();

        public bool IsShow
        {
            get => _isShow;
            private set => SetProperty(ref _isShow, value);
        }

        public ICommand ChangeTabCommand => new RelayCommand<int>(index => SelectedTabItemIndex = index);

        public ICommand CloseCommand => new RelayCommand(Close);

        public event EventHandler PanelOpened;
        public event EventHandler PanelClosed;


        public void SetProgress(string status, double progress)
        {
            ProgressData.IsShow = progress >= 0 && progress <= 100;
            ProgressData.SetProgress(status, progress);
        }

        public void UpdateVersionToToday()
        {
            Version = Version.Split(new[] { '@' }, 2)[0] + '@' + DateTime.Now.ToString("yyMMdd");
        }

        private bool IsVaildAPIServerDirectory(string path)
        {
            var infoFilePath = Path.Combine(path, URLs.InfoFilename);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(infoFilePath))
            {
                try
                {
                    LoadServerInfo(infoFilePath);
                }
                catch (Exception)
                {
                    return false;
                }

                return Version != null && WelcomeMessage != null;
            }

            return false;
        }

        public bool ResetAPIServerDirectory()
        {
            var setting = Settings.Default;
            var path = CommonUtils.SelectDirectory("API Server의 Root폴더 선택");

            if (path == null)
                return false;

            if (!IsVaildAPIServerDirectory(path))
            {
                MessageBox.Show("선택한 폴더는 올바른 API Server폴더가 아닙니다.");
                return false;
            }

            setting.APIServerDirectory = path;
            setting.Save();
            return true;
        }

        private void LoadServerInfo(string infoFilePath)
        {
            var json = JObject.Parse(File.ReadAllText(infoFilePath));
            Version = json.Value<string>("patchVersion");
            WelcomeMessage = json.Value<string>("notice");
        }

        private void SaveServerInfo()
        {
            var path = Settings.Default.APIServerDirectory;
            var json = new JObject(new JProperty("patchVersion", Version), new JProperty("notice", WelcomeMessage));
            File.WriteAllText(Path.Combine(path, URLs.InfoFilename), json.ToString());
        }

        public void Open()
        {
            if (!IsShow)
            {
                if (!IsVaildAPIServerDirectory(Settings.Default.APIServerDirectory) && !ResetAPIServerDirectory())
                    return;
                PanelOpened?.Invoke(this, null);
                IsShow = true;
            }
        }

        public void Close()
        {
            if (IsShow)
            {
                Settings.Default.Save();
                SaveServerInfo();
                IsShow = false;
                PanelClosed?.Invoke(this, null);
            }
        }
    }
}