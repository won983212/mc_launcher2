using Minecraft_Launcher_2.Controls;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.Updater.ServerConnections;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_Launcher_2.Updater
{
    public class UpdaterViewModel : ObservableObject
    {
        private readonly MinecraftLauncher _launcher;
        private bool _running = false;

        public UpdaterViewModel(ServerDataContext context)
        {
            _launcher = new MinecraftLauncher(context);
            _launcher.OnLog += (s, t) => Logger.Info(t);
            _launcher.OnError += (s, t) => Logger.Error(t);
            _launcher.OnExited += (s, t) => Logger.Info(" Exited (code: " + t + ")");
        }

        public async Task StartMinecraft()
        {
            Settings settings = Settings.Default;
            _launcher.PlayerName = settings.PlayerName;
            settings.Save();

            IsRunning = true;
            _launcher.IsAutoJoin = _launcher.Context.Retriever.ConnectionState.State == RetrieveState.Loaded && settings.AutoJoinServer;

            await _launcher.Start();
            IsRunning = false;

            if (Settings.Default.UseLogging)
                return;

            App.Current.Shutdown(0);
        }

        public async Task StartDownload()
        {
            IsRunning = true;
            ProgressData.IsShow = true;
            ProgressData.SetProgress("다운로드 중..", 0);

            ContentUpdater updater = new ContentUpdater();
            updater.OnProgress += Updater_OnProgress;
            int failed = await updater.BeginDownload();

            ProgressData.IsShow = false;
            _launcher.Context.ReadInstalledPatchVersion();

            if (failed > 0)
            {
                MessageBoxResult res = MessageBox.Show("파일 " + failed + "개를 받지 못했습니다. 그래도 실행합니까?", "주의", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                    StartMinecraft();
                else
                    IsRunning = false;
            }
            else
                StartMinecraft();
        }

        private void Updater_OnProgress(object sender, ProgressArgs e)
        {
            ProgressData.SetProgress(e.Status, e.Progress);
        }

        public ProgressStatus ProgressData { get; } = new ProgressStatus();

        public bool IsRunning
        {
            get => _running;
            private set => SetProperty(ref _running, value);
        }
    }
}
