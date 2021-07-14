using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_Launcher_2.Updater
{
    public class UpdaterViewModel : ObservableObject
    {
        private readonly MinecraftLauncher _launcher;

        private bool _isShowDownloadStatus = false;
        private double _downloadProgress = 0;
        private string _downloadStatus = "";
        private bool _running = false;

        public UpdaterViewModel(LauncherContext context)
        {
            _launcher = new MinecraftLauncher(context);
            _launcher.OnLog += (s, t) => Logger.Log(t);
            _launcher.OnError += (s, t) => Logger.Error(t);
            _launcher.OnExited += (s, t) => Logger.Log(" Exited (code: " + t + ")");
        }

        public async Task StartMinecraft()
        {
            Settings settings = Settings.Default;
            _launcher.PlayerName = settings.PlayerName;
            settings.Save();

            IsRunning = true;
            await _launcher.Start();
            IsRunning = false;

            if (Settings.Default.UseLogging)
                return;

            App.Current.Shutdown(0);
        }

        public async Task StartDownload()
        {
            IsShowDownloadStatus = true;
            IsRunning = true;
            DownloadStatus = "다운로드 중..";
            DownloadProgress = 0;

            ContentUpdater updater = new ContentUpdater();
            updater.OnProgress += Updater_OnProgress;
            int failed = await updater.BeginDownload();

            IsShowDownloadStatus = false;
            _launcher.Context.GetInstalledPatchVersion();

            if (failed > 0)
            {
                MessageBoxResult res = MessageBox.Show("파일 " + failed + "개를 받지 못했습니다. 그래도 실행합니까?", "주의", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    StartMinecraft();
                }
                else
                {
                    IsRunning = false;
                }
            }
            else
            {
                StartMinecraft();
            }
        }

        private void Updater_OnProgress(object sender, ProgressArgs e)
        {
            DownloadStatus = e.Status;
            DownloadProgress = e.Progress;
        }

        public bool IsShowDownloadStatus
        {
            get => _isShowDownloadStatus;
            private set => SetProperty(ref _isShowDownloadStatus, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            private set => SetProperty(ref _downloadProgress, value);
        }

        public string DownloadStatus
        {
            get => _downloadStatus;
            private set => SetProperty(ref _downloadStatus, value);
        }

        public bool IsRunning
        {
            get => _running;
            private set => SetProperty(ref _running, value);
        }
    }
}
