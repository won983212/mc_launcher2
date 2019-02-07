using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
	class LauncherProfile
	{
		private const char spliter = '♩';

		#region 공용 LauncherProfile
		static LauncherProfile()
		{
			ClientProfile = new LauncherProfile();
			ClientProfile.ProfilePath = Path.Combine(Properties.Settings.Default.Minecraft_Dir, "launcher_profile.dat");
		}

		public static LauncherProfile ClientProfile { get; private set; }
		#endregion

		#region 실제 Data Properties
		public string ProfilePath { get; set; }
		public string PatchVersion { get; set; }
		public string MinecraftVersion { get; set; }
		public string LaunchArguments { get; set; }
		public string MainClass { get; set; }
		public string AssetsVersion { get; set; }
		public string AssetsJsonUrl { get; set; }
		public string LibraryData { get; private set; }
		public string ClientVersion
		{
			get
			{
				if (MinecraftVersion == null || PatchVersion == null)
					return "설치 필요";
				return MinecraftVersion + "#" + PatchVersion;
			}
		}
		#endregion

		// libs는 오직 업데이트할 때만 사용한다. Save할 때 저장도 하지 않는 데이터이다.
		private List<Library> libs = null;
		public List<Library> Libraries
		{
			get
			{
				return libs;
			}

			set
			{
				libs = value;
				LibraryData = Library.SerializeList(libs);
			}
		}

		public bool Loaded
		{
			get
			{
				return PatchVersion != null;
			}
		}

		private void WriteToken(StringBuilder writer, string data)
		{
			if (data == null)
				throw new InvalidDataException("null인 데이터가 존재합니다.");

			writer.Append(data);
			writer.Append(spliter);
		}

		public void Reset()
		{
			PatchVersion = null;
			MinecraftVersion = null;
			LaunchArguments = null;
			MainClass = null;
			AssetsVersion = null;
			AssetsJsonUrl = null;
			LibraryData = null;
			libs = null;
		}

		public void Load(LauncherProfile profile)
		{
			PatchVersion = profile.PatchVersion;
			MinecraftVersion = profile.MinecraftVersion;
			LaunchArguments = profile.LaunchArguments;
			MainClass = profile.MainClass;
			AssetsVersion = profile.AssetsVersion;
			AssetsJsonUrl = profile.AssetsJsonUrl;
			LibraryData = profile.LibraryData;
		}

		public bool Load()
		{
			// profile 파일이 존재하지 않거나 parent folder가 없는 경우.
			if (!(Directory.Exists(Directory.GetParent(ProfilePath).FullName) && File.Exists(ProfilePath)))
			{
				MainWindow.Monitor.Error("Load launcher setting failed: profile파일을 찾을 수 없습니다.");
				return false;
			}

			string[] datas = File.ReadAllText(ProfilePath).Split(spliter);
			if (datas.Length != 7) // 데이터의 개수가 안 맞는 경우
			{
				MainWindow.Monitor.Error("Load launcher setting failed: 저장된 데이터의 개수가 잘못되었습니다." );
				return false;
			}

			try
			{
				PatchVersion = datas[0];
				MinecraftVersion = datas[1];
				LaunchArguments = datas[2];
				MainClass = datas[3];
				AssetsVersion = datas[4];
				AssetsJsonUrl = datas[5];
				LibraryData = datas[6];
			}
			catch (Exception e)
			{
				MainWindow.Monitor.Error("Load launcher setting failed\n" + e.ToString());
				return false;
			}

			return true;
		}

		public void Save()
		{
			Directory.CreateDirectory(Directory.GetParent(ProfilePath).FullName);
			StringBuilder sb = new StringBuilder();
			WriteToken(sb, PatchVersion);
			WriteToken(sb, MinecraftVersion);
			WriteToken(sb, LaunchArguments);
			WriteToken(sb, MainClass);
			WriteToken(sb, AssetsVersion);
			WriteToken(sb, AssetsJsonUrl);
			WriteToken(sb, LibraryData);

			string data = sb.ToString();
			File.WriteAllText(ProfilePath, data.Substring(0, data.Length - 1));
		}
	}
}
