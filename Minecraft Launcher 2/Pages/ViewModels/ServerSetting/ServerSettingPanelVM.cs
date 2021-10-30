using Minecraft_Launcher_2.Controls;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.ServerConnections;
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

        private void SaveServerInfo()
        {
            var path = Settings.Default.APIServerDirectory;
            var json = new JObject(new JProperty("patchVersion", Version), new JProperty("notice", WelcomeMessage));
            File.WriteAllText(Path.Combine(path, URLs.InfoFilename), json.ToString());
        }

        public bool ResetAPIServerDirectory()
        {
            APIServerInfoRetriever retriever = CommonUtils.ResetAPIServerDirectory();
            if (retriever == null)
                return false;

            Version = retriever.PatchVersion;
            WelcomeMessage = retriever.Notice;
            return true;
        }

        public void Open()
        {
            if (!IsShow)
            {
                APIServerInfoRetriever retriever = new APIServerInfoRetriever();
                if (!retriever.RetrieveFromAPIServerDirectory(Settings.Default.APIServerDirectory) && (retriever = CommonUtils.ResetAPIServerDirectory()) == null)
                    return;

                Version = retriever.PatchVersion;
                WelcomeMessage = retriever.Notice;

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