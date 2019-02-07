using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
	class MultipleDownloader : NotifyProcess
	{
		public static readonly int MAX_DOWNLOAD_PROCESS = 30;
		public static readonly int MAX_DOWNLOAD_RETRY = 3;

		private int downloadedCount = 0;
		private int failed = 0;
		private List<DownloadEntry> downloads;

		public MultipleDownloader(List<DownloadEntry> list)
		{
			downloads = list;
		}

		public void StartDownload()
		{
			FireCurrentProgress(0, "");
			Task.Factory.StartNew(() => Parallel.ForEach(downloads, new ParallelOptions { MaxDegreeOfParallelism = MAX_DOWNLOAD_PROCESS }, DownloadFile));
		}

		public int GetFailedCount()
		{
			return failed;
		}

		private void DownloadFile(DownloadEntry ent)
		{
			if (!Directory.Exists(ent.path))
				Directory.CreateDirectory(Directory.GetParent(ent.path).FullName);

			int retry = 0;
			MainWindow.Monitor.Info("Start Download: " + ent.url);

			for (; retry < MAX_DOWNLOAD_RETRY; retry++)
			{
				try
				{
					using (WebClient client = new WebClient())
						client.DownloadFile(ent.url, ent.path);

					if (ent.nativeZipExcludes != null)
					{
						string path = Path.Combine(Properties.Settings.Default.Minecraft_Dir, "libraries", "natives");
						using (ZipArchive zip = ZipFile.OpenRead(ent.path))
						{
							Directory.CreateDirectory(path);
							foreach (ZipArchiveEntry zipent in zip.Entries)
							{
								foreach (string name in ent.nativeZipExcludes)
								{
									if (zipent.FullName.StartsWith(name))
									{
										continue;
									}
								}
								if (zipent.FullName.EndsWith("/"))
								{
									Directory.CreateDirectory(path);
								}
								else
								{
									string extractpath = Path.Combine(path, zipent.Name);
									if (File.Exists(extractpath))
									{
										File.Delete(extractpath);
									}
									zipent.ExtractToFile(extractpath);
								}
							}
							MainWindow.Monitor.Info("Extracted native: " + path);
						}
						File.Delete(ent.path);
					}

					MainWindow.Monitor.Info("Finish Download: " + ent.url);
					break;
				}
				catch (Exception e)
				{
					WebException exception = new WebException(ent.url, e);
					FireOnException(exception);
					MainWindow.Monitor.Info("Failed to download " + ent.url + ". (try " + (retry + 1) + ")");
				}
			}

			if (retry == MAX_DOWNLOAD_RETRY)
				failed++;

			FireCurrentProgress((double)(++downloadedCount * 100) / downloads.Count, Path.GetFileName(ent.path));

			if (downloadedCount >= downloads.Count)
				FireOnComplete(failed);
		}
	}

	public class DownloadEntry
	{
		public string path;
		public string url;
		public string[] nativeZipExcludes;
	}
}
