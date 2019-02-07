using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
	[Serializable]
	class Library
	{
		private const char spliter = '☎';

		public Library(string downloadURL, string path, string[] unzipExcludes)
		{
			Uri uri = new Uri(downloadURL);

			DownloadURL = downloadURL;
			FilePath = path;
			UnzipExcludes = unzipExcludes;
		}

		public string[] UnzipExcludes { get; private set; } = null;
		public string FilePath { get; private set; }
		public string DownloadURL { get; private set; }
		public string FileName
		{
			get
			{
				return Path.GetFileName(FilePath);
			}
		}
		public bool IsNativeJar
		{
			get
			{
				return UnzipExcludes != null;
			}
		}

		public DownloadEntry CreateDownloadEntry()
		{
			DownloadEntry ent = new DownloadEntry();
			ent.path = FilePath;
			ent.url = DownloadURL;
			ent.nativeZipExcludes = UnzipExcludes;
			return ent;
		}

		public static string SerializeList(List<Library> libs)
		{
			StringBuilder sb = new StringBuilder();
			string libdir = Path.Combine(Properties.Settings.Default.Minecraft_Dir, "libraries");
			foreach(Library lib in libs)
			{
				sb.Append(lib.FilePath.Substring(libdir.Length + 1));
				sb.Append(spliter);
			}

			string str = sb.ToString();
			return str.Substring(0, str.Length - 1);
		}

		public static List<string> DeserializeString(string str)
		{
			return new List<string>(str.Split(spliter));
		}
	}
}
