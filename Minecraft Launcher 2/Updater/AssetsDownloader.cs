using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
	class AssetsDownloader : NotifyProcess
	{
		public void DownloadAssets(string assetsFolder, string assetsVersion, string indexURL)
		{
			MainWindow.Monitor.Info("Assets 다운로드 시작");

			AssetsInfoArg info = new AssetsInfoArg(assetsFolder, assetsVersion);
			WebClient client = new WebClient();

			MainWindow.Monitor.Info("Assets Index 다운로드 시작");
			client.DownloadProgressChanged += AssetsIndexProgress;
			client.DownloadStringCompleted += AssetsIndexDownloaded;
			client.DownloadStringAsync(new Uri(indexURL), info);
		}

		private void AssetsIndexDownloaded(object sender, DownloadStringCompletedEventArgs e)
		{
			string assetsFolder = ((AssetsInfoArg)e.UserState).dir;
			string assetsVersion = ((AssetsInfoArg)e.UserState).version;
			string jsontext = e.Result;

			JObject obj = JObject.Parse(jsontext);
			JToken token = obj["objects"];
			List<DownloadEntry> downloads = new List<DownloadEntry>();

			string indexfile = Path.Combine(assetsFolder, "indexes", assetsVersion + ".json");
			if (!Directory.Exists(indexfile))
				Directory.CreateDirectory(Directory.GetParent(indexfile).FullName);

			using (StreamWriter sw = new StreamWriter(indexfile))
				sw.WriteLine(jsontext);

			foreach (JProperty property in token)
			{
				string hash = (string)property.Value["hash"];
				string path = Path.Combine(assetsFolder, "objects", hash.Substring(0, 2), hash);
				DownloadEntry ent = new DownloadEntry();

				ent.path = path;
				ent.url = Properties.Settings.Default.AssetsURL + hash.Substring(0, 2) + "/" + hash;

				if (!Directory.Exists(path))
					Directory.CreateDirectory(Directory.GetParent(path).FullName);

				downloads.Add(ent);
			}

			MultipleDownloader downloader = new MultipleDownloader(downloads);
			downloader.OnException += (o, i) => { FireOnException(i); };
			downloader.OnComplete += (o, i) => { FireOnComplete(i); };
			downloader.CurrentDownloadProgress += AssetsTotalDownloadProgress;
			downloader.StartDownload();
		}

		private void AssetsTotalDownloadProgress(object sender, Tuple<double, string> e)
		{
			if (e.Item1 == 100)
			{
				int failed = ((MultipleDownloader)sender).GetFailedCount();
				if (failed == 0)
					FireCurrentProgress(e.Item1, "Assets 다운로드 완료.");
				else
					FireCurrentProgress(e.Item1, "Assets 다운로드 완료. (" + failed + "개 실패)");
			}
			else FireCurrentProgress(e.Item1, "Assets 파일 다운로드중..");
		}

		private void AssetsIndexProgress(object sender, DownloadProgressChangedEventArgs e)
		{
			if (e.ProgressPercentage == 100)
				FireCurrentProgress(e.ProgressPercentage, "Assets index 파일 다운로드 완료.");
			else
				FireCurrentProgress(e.ProgressPercentage, "Assets index 파일 다운로드중..");
		}

		private class AssetsInfoArg
		{
			public string dir;
			public string version;

			public AssetsInfoArg(string dir, string version)
			{
				this.dir = dir;
				this.version = version;
			}
		}
	}
}
