using Minecraft_Launcher_2.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
    public class ContentUpdater
    {
        private static readonly Settings settings = Settings.Default;

        private volatile HashDownloader _currentDownloader;
        public event EventHandler<ProgressArgs> OnProgress;

        public async Task<int> BeginDownload()
        {
            try
            {
                UpdateProgress(0, "Launch Config 가져오는중..");
                var assetsURL = await RetrieveAssetsIndex();
                var failed = 0;

                _currentDownloader = new HashDownloader(Path.Combine(settings.MinecraftDir, "assets"), assetsURL,
                    URLs.AssetsResourceUrl);
                _currentDownloader.OnProgress += (s, a) =>
                {
                    UpdateProgress(a.Progress / 2 + 10, "에셋파일: " + a.Status);
                };
                _currentDownloader.UseHashPath = true;
                failed += await _currentDownloader.DownloadTask();
                CheckDownloadTaskCancelled();

                _currentDownloader =
                    new HashDownloader(settings.MinecraftDir, URLs.IndexFilePath, URLs.PatchFolderPath);
                _currentDownloader.DetectDeletionFolder = new[] { "mods", "libraries", "natives", "scripts" };
                _currentDownloader.OnProgress += (s, a) =>
                {
                    UpdateProgress(a.Progress * 0.39 + 60, "패치파일: " + a.Status);
                };
                failed += await _currentDownloader.DownloadTask();
                CheckDownloadTaskCancelled();

                _currentDownloader = null;

                UpdateProgress(99, "버전 정보 수정중..");
                await Task.Factory.StartNew(UpdatePatchVersion);

                UpdateProgress(100, "설치완료");
                return failed;
            }
            catch (Exception)
            {
                UpdateProgress(100, "작업 취소됨");
                throw;
            }
        }

        private void CheckDownloadTaskCancelled()
        {
            if (_currentDownloader == null)
                throw new TaskCanceledException();
        }

        public void Cancel()
        {
            if (_currentDownloader != null)
            {
                _currentDownloader.Cancel();
                _currentDownloader = null;
            }
        }

        private void UpdatePatchVersion()
        {
            var patchVersionPath = Path.Combine(settings.MinecraftDir, "version");
            using (var client = new WebClient { Encoding = Encoding.UTF8 })
            {
                var data = client.DownloadString(URLs.InfoFilePath);
                var json = JObject.Parse(data);
                File.WriteAllText(patchVersionPath, (string)json["patchVersion"]);
            }
        }

        private async Task<string> RetrieveAssetsIndex()
        {
            var client = new WebClient();
            client.DownloadProgressChanged += (s, e) =>
            {
                UpdateProgress(0.1 * e.ProgressPercentage, "Launch Config 가져오는중");
            };

            var cfg = await client.DownloadStringTaskAsync(URLs.LauncherConfigPath);
            client.Dispose();

            // save launcher-config file
            Directory.CreateDirectory(settings.MinecraftDir);
            File.WriteAllText(Path.Combine(settings.MinecraftDir, Path.GetFileName(URLs.LauncherConfigPath)), cfg);

            var cfgJson = JObject.Parse(cfg);
            return (string)cfgJson["assetsUrl"];
        }

        private void UpdateProgress(double progress, string status)
        {
            OnProgress?.Invoke(this, new ProgressArgs(progress, status));
        }
    }
}