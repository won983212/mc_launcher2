using System;
using System.Threading.Tasks;
using System.Windows;
using Minecraft_Launcher_2.Controls;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;

namespace Minecraft_Launcher_2.Updater
{
    public class UpdaterViewModel : ObservableObject
    {
        private readonly MinecraftLauncher _launcher;
        private bool _running;

        public UpdaterViewModel(ServerDataContext context)
        {
            _launcher = new MinecraftLauncher(context);
            _launcher.OnLog += (s, t) => Logger.Info(t);
            _launcher.OnError += (s, t) => Logger.Error(t);
            _launcher.OnExited += (s, t) => Logger.Info(" Exited (code: " + t + ")");
        }

        public ProgressStatus ProgressData { get; } = new ProgressStatus();

        public bool IsRunning
        {
            get => _running;
            private set => SetProperty(ref _running, value);
        }

        public async Task StartMinecraft(bool useAutoJoin)
        {
            var settings = Settings.Default;
            _launcher.PlayerName = settings.PlayerName;
            settings.Save();

            IsRunning = true;
            _launcher.IsAutoJoin = useAutoJoin;

            await _launcher.Start();
            IsRunning = false;
        }

        public async Task<bool> StartDownload()
        {
            IsRunning = true;
            ProgressData.IsShow = true;
            ProgressData.SetProgress("다운로드 중..", 0);

            var updater = new ContentUpdater();
            updater.OnProgress += Updater_OnProgress;

            var failed = 0;
            try
            {
                failed = await updater.BeginDownload();
                _launcher.Context.ReadInstalledPatchVersion();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "업데이트 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error(e);
                IsRunning = false;
                return false;
            }
            finally
            {
                ProgressData.IsShow = false;
            }

            IsRunning = false;
            if (failed > 0)
            {
                var res = MessageBox.Show("파일 " + failed + "개를 받지 못했습니다. 그래도 실행합니까?", "주의", MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        private void Updater_OnProgress(object sender, ProgressArgs e)
        {
            ProgressData.SetProgress(e.Status, e.Progress);
        }
    }
}