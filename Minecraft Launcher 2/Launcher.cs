using Minecraft_Launcher_2.Updater;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_Launcher_2
{
	class LaunchSetting
	{
		public string MainClass { get; private set; }
		public string MinecraftArguments { get; private set; }
		public string AssetsVersion { get; private set; }
		public string MinecraftVersion { get; private set; }
		public List<string> Libraries { get; private set; } = new List<string>();

		public void Load(string settingFile)
		{
			string data = File.ReadAllText(settingFile);
			JObject json = JObject.Parse(data);

			LauncherContext ctx = App.MainContext;
			ctx.UpdatePatchVersion();

			MainClass = (string)json["mainClass"];
			MinecraftArguments = (string)json["minecraftArguments"];
			AssetsVersion = (string)json["assets"];
			MinecraftVersion = ctx.PatchVersion.Split('@')[0];

			JArray libs = json["libraries"] as JArray;
			foreach (JObject obj in libs)
			{
				string name = (string)obj["name"];
				string[] names = name.Split(':');
				string jarfile = names[0].Replace('.', '\\') + '\\' + names[1] + '\\' + names[2] + '\\' + names[1] + "-" + names[2] + ".jar";
				Libraries.Add(Path.Combine(Properties.Settings.Default.MinecraftDir, "libraries", jarfile));
			}
		}
	}

	public class Launcher
	{
		private static readonly Properties.Settings settings = Properties.Settings.Default;
		private volatile bool _isRunning = false;

		public event EventHandler<string> OnLog;
		public event EventHandler<string> OnError;
		public event EventHandler<int> OnExited;

		public string PlayerName { get; set; } = "Unnamed";
		public bool IsRunning { get => _isRunning; }

		private string GetLaunchAdditionalArguments(LaunchSetting launchSettings)
		{
			string arg = launchSettings.MinecraftArguments;
			arg = arg.Replace("${auth_player_name}", PlayerName);
			arg = arg.Replace("${version_name}", launchSettings.MinecraftVersion);
			arg = arg.Replace("${game_directory}", settings.MinecraftDir);
			arg = arg.Replace("${assets_root}", Path.Combine(settings.MinecraftDir, "assets"));
			arg = arg.Replace("${assets_index_name}", launchSettings.AssetsVersion);
			arg = arg.Replace("${auth_uuid}", "sessionid");
			arg = arg.Replace("${auth_access_token}", "-");
			arg = arg.Replace("${user_type}", "-");
			return arg;
		}

		private string GetArguments()
		{
			Log("Extracting launcher info....");
			LaunchSetting launchSettings = new LaunchSetting();
			launchSettings.Load(Path.Combine(settings.MinecraftDir, "launch-config.json"));

			Log("Building arguments....");

			StringBuilder sb = new StringBuilder();
			sb.Append(settings.Arguments);
			sb.Append(" -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump ");
			sb.Append("-Xmx");
			sb.Append(settings.MemorySize);
			sb.Append("G ");
			sb.Append("-Djava.library.path=");
			sb.Append(Path.Combine(settings.MinecraftDir, "natives"));
			sb.Append(" -cp ");

			foreach (string lib in launchSettings.Libraries)
			{
				sb.Append(Path.Combine(settings.MinecraftDir, "libraries", lib));
				sb.Append(';');
			}

			sb.Append(Path.Combine(settings.MinecraftDir, "minecraft.jar"));
			sb.Append(' ');
			sb.Append(launchSettings.MainClass);
			sb.Append(' ');
			sb.Append(GetLaunchAdditionalArguments(launchSettings));
			sb.Append(" --server ");
			sb.Append(settings.MinecraftServerIP);
			sb.Append(" --port ");
			sb.Append(settings.MinecraftServerPort);

			return sb.ToString();
		}

		public Task Start()
		{
			return Task.Factory.StartNew(StartSync);
		}

		private void StartSync()
		{
			_isRunning = true;
			try
			{
				Process p = new Process();
				ProcessStartInfo info = new ProcessStartInfo();

				info.FileName = "java";
				info.Arguments = GetArguments();
				info.WorkingDirectory = settings.MinecraftDir;
				info.CreateNoWindow = true;
				info.UseShellExecute = false;

				if (settings.UseLogging)
				{
					info.RedirectStandardOutput = true;
					info.RedirectStandardError = true;

					p.OutputDataReceived += (sender, ar) => Log(ar.Data);
					p.ErrorDataReceived += (sender, ar) => Error(ar.Data);
				}

				Log("Starting process...");

				p.StartInfo = info;
				p.Start();

				if (settings.UseLogging)
				{
					p.BeginOutputReadLine();
					p.BeginErrorReadLine();

					p.WaitForExit();
					OnExited?.Invoke(this, p.ExitCode);
					_isRunning = false;
				}
				else
				{
					Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown(0));
				}
			}
			catch (Exception e)
			{
				Error(e.ToString());
			}
		}

		private void Log(string str)
		{
			OnLog?.Invoke(this, str);
		}

		private void Error(string str)
		{
			OnError?.Invoke(this, str);
		}
	}
}
