using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_Launcher_2.Updater
{
	class MinecraftPatcher : NotifyProcess
	{
		private static Properties.Settings settings = Properties.Settings.Default;
		private string latestPatch = null;
		private LauncherProfile serverProfile = new LauncherProfile();

		public MinecraftPatcher(string latestPatchVersion)
		{
			latestPatch = latestPatchVersion ?? throw new InvalidDataException("서버와 연결하지 않고 업데이트를 시도하였습니다.");
		}

		public async void UpdateAsync()
		{
			try
			{
				FireCurrentProgress(0, "");
				FireTotalProgress(0, "업데이트 정보 받아오는 중..");
				await Task.Run(() => LoadServerLaunchSettings());

				FireTotalProgress(10, "Assets 다운로드 하는 중..");
				TryDownloadAssets();
			}
			catch (Exception e)
			{
				HandleException(e);
			}
		}

		// Assets를 설치해야하는지 판단한다. 설치해야한다면 assets폴더 제거 후 설치한다.
		private void TryDownloadAssets()
		{
			string assetsPath = Path.Combine(settings.Minecraft_Dir, "assets");

			// 게임이 설치되어있고, assets 버전이 같으면 설치하지 않는다.
			if (LauncherProfile.ClientProfile.Loaded && LauncherProfile.ClientProfile.AssetsVersion == serverProfile.AssetsVersion)
			{
				MainWindow.Monitor.Info("Skip assets download.");
				AssetsDownloader_OnComplete(null, 0);
				return;
			}

			FireCurrentProgress(0, "Assets폴더 삭제 중..");
			if (Directory.Exists(assetsPath))
				Directory.Delete(assetsPath, true); // assets폴더 삭제

			// assets 다운로드
			AssetsDownloader assetsDownloader = new AssetsDownloader();
			assetsDownloader.OnException += (o, e) => FireOnException(e);
			assetsDownloader.CurrentDownloadProgress += (o, e) => FireCurrentProgress(e.Item1, e.Item2);
			assetsDownloader.TotalDownloadProgress += (o, e) => FireTotalProgress(e.Item1, e.Item2);
			assetsDownloader.OnComplete += AssetsDownloader_OnComplete;
			assetsDownloader.DownloadAssets(assetsPath, serverProfile.AssetsVersion, serverProfile.AssetsJsonUrl);
		}

		private void AssetsDownloader_OnComplete(object sender, int e)
		{
			try
			{
				List<DownloadEntry> libraries = new List<DownloadEntry>();
				foreach (Library lib in serverProfile.Libraries)
				{
					if (!File.Exists(lib.FilePath))
						libraries.Add(lib.CreateDownloadEntry());
				}

				FireTotalProgress(40, "라이브러리 다운로드 중..");

				MultipleDownloader libDownloader = new MultipleDownloader(libraries);
				libDownloader.OnException += (o, ev) => FireOnException(ev);
				libDownloader.OnComplete += LibraryDownloader_OnComplete;
				libDownloader.CurrentDownloadProgress += LibraryDownloader_CurrentDownloadProgress;
				libDownloader.StartDownload();
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		private void LibraryDownloader_CurrentDownloadProgress(object sender, Tuple<double, string> e)
		{
			if (e.Item1 == 100)
			{
				int failed = ((MultipleDownloader)sender).GetFailedCount();
				if (failed == 0)
				{
					FireCurrentProgress(e.Item1, "라이브러리 다운로드 완료.");
				}
				else
				{
					FireCurrentProgress(e.Item1, "라이브러리 다운로드 완료. (" + failed + "개 실패)");
				}

				MainWindow.Monitor.Info("라이브러리 다운로드 완료. (" + failed + "개 실패)");
			}
			else
			{
				FireCurrentProgress(e.Item1, e.Item2);
			}
		}

		private void LibraryDownloader_OnComplete(object sender, int e)
		{
			try
			{
				TimeoutWebClient client = new TimeoutWebClient();
				client.Timeout = 60000;

				string patchPath = Path.Combine(settings.UpdateHost, "patch.zip");
				string tempLocation = Path.GetTempFileName();

				FireTotalProgress(60, "패치 데이터 다운로드 중..");
				client.DownloadFileCompleted += Client_DownloadFileCompleted;
				client.DownloadProgressChanged += (s, ev) => FireCurrentProgress((double)ev.BytesReceived * 100 / ev.TotalBytesToReceive, "patch.zip 다운로드중..");
				client.DownloadFileAsync(new Uri(patchPath), tempLocation, tempLocation);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			try
			{
				FireCurrentProgress(0, "patch.zip 압축푸는 중..");
				FireTotalProgress(80, "패치중..");

				Task.Factory.StartNew(ProcessUnzipAsync, e.UserState);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		private void ProcessUnzipAsync(object data)
		{
			try
			{
				string zipfile = (string)data;
				int progress = 0;

				using (ZipArchive zip = ZipFile.OpenRead(zipfile))
				{
					foreach (ZipArchiveEntry zipent in zip.Entries)
					{
						if (zipent.FullName.EndsWith("/"))
						{
							Directory.CreateDirectory(Path.Combine(settings.Minecraft_Dir, zipent.FullName));
						}
						else
						{
							string zipentPath = Path.Combine(settings.Minecraft_Dir, zipent.FullName);

							if (File.Exists(zipentPath))
							{
								File.Delete(zipentPath);
							}

							zipent.ExtractToFile(zipentPath);
						}

						FireCurrentProgress(++progress * 100 / (double)zip.Entries.Count, "patch.zip 압축푸는 중..");
					}
				}

				LauncherProfile.ClientProfile.Load(serverProfile);
				LauncherProfile.ClientProfile.Save();
			}
			catch (Exception e)
			{
				HandleException(e);
			}

			FireTotalProgress(100, "패치완료!");
			FireOnComplete(0);
		}

		private void HandleException(Exception e)
		{
			MessageBox.Show("패치도중 오류가 발생하였습니다. 자세한 내용은 콘솔에 표시됩니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
			FireOnException(e);
		}

		// 서버로부터 launch셋팅을 가져온다.
		private void LoadServerLaunchSettings()
		{
			string settingPath = Path.Combine(settings.UpdateHost, "launch_setting.txt");
			using (TimeoutWebClient client = new TimeoutWebClient())
			{
				client.Timeout = 10000;
				using (Stream stream = client.OpenRead(settingPath))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						serverProfile.PatchVersion = latestPatch;
						serverProfile.MinecraftVersion = reader.ReadLine();
						serverProfile.LaunchArguments = reader.ReadLine();
						serverProfile.MainClass = reader.ReadLine();
						serverProfile.AssetsVersion = reader.ReadLine();
						serverProfile.AssetsJsonUrl = reader.ReadLine();

						string buffer = null;
						string[] unzipExcludes = null;
						List<Library> libs = new List<Library>();
						while ((buffer = reader.ReadLine()) != null)
						{
							if (buffer.StartsWith(","))
							{
								unzipExcludes = buffer.Substring(1).Split(',');
							}
							else
							{
								string[] pathSplit = buffer.Split(' ');
								string path = Path.Combine(settings.Minecraft_Dir, "libraries", pathSplit[1].Replace('/', '\\'));

								Library lib = new Library(pathSplit[0] + pathSplit[1], path, unzipExcludes);
								libs.Add(lib);

								unzipExcludes = null;
							}
						}
						serverProfile.Libraries = libs;
					}
				}
			}
		}
	}
}
