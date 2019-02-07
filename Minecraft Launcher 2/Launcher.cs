using Minecraft_Launcher_2.Updater;
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
	class Launcher
	{
		private static readonly Properties.Settings settings = Properties.Settings.Default;
		private static readonly LauncherProfile launchSettings = LauncherProfile.ClientProfile;
		private volatile bool isRunning = false;
		public event EventHandler Exit;

		private string GetLaunchAdditionalArguments()
		{
			string arg = launchSettings.LaunchArguments;
			arg = arg.Replace("${auth_player_name}", settings.LastLogined);
			arg = arg.Replace("${version_name}", launchSettings.MinecraftVersion);
			arg = arg.Replace("${game_directory}", settings.Minecraft_Dir);
			arg = arg.Replace("${assets_root}", Path.Combine(settings.Minecraft_Dir, "assets"));
			arg = arg.Replace("${assets_index_name}", launchSettings.AssetsVersion);
			arg = arg.Replace("${auth_uuid}", "sessionid");
			arg = arg.Replace("${auth_access_token}", "-");
			arg = arg.Replace("${user_type}", "-");
			return arg;
		}

		private string GetArguments()
		{
			string libdir = Path.Combine(settings.Minecraft_Dir, "libraries");

			StringBuilder sb = new StringBuilder();
			sb.Append(settings.Arguments);
			sb.Append(' ');
			sb.Append("-Xmx");
			sb.Append(settings.MemorySize);
			sb.Append("G ");
			sb.Append("-Djava.library.path=");
			sb.Append(Path.Combine(libdir, "natives"));
			sb.Append(" -cp ");

			foreach (string lib in Library.DeserializeString(launchSettings.LibraryData))
			{
				sb.Append(Path.Combine(libdir, lib));
				sb.Append(';');
			}

			sb.Append(Path.Combine(settings.Minecraft_Dir, "minecraft.jar"));
			sb.Append(' ');
			sb.Append(launchSettings.MainClass);
			sb.Append(' ');
			sb.Append(GetLaunchAdditionalArguments());

			return sb.ToString();
		}

		public bool IsRunning()
		{
			return isRunning;
		}

		public void Start()
		{
			try
			{
				Process p = new Process();
				ProcessStartInfo info = new ProcessStartInfo();

				info.FileName = "java";
				info.Arguments = GetArguments();
				info.WorkingDirectory = settings.Minecraft_Dir;
				info.CreateNoWindow = true;
				info.UseShellExecute = false;

				if (settings.UseDebug)
				{
					MainWindow.Monitor.ShowWindow();
					MainWindow.Monitor.Info("Command: " + info.Arguments);

					info.RedirectStandardOutput = true;
					info.RedirectStandardError = true;

					p.OutputDataReceived += (sender, ar) => OnOutput(ar.Data);
					p.ErrorDataReceived += (sender, ar) => OnError(ar.Data);
				}

				isRunning = true;
				p.StartInfo = info;
				p.Start();

				if (settings.UseDebug)
				{
					p.BeginOutputReadLine();
					p.BeginErrorReadLine();

					ThreadPool.QueueUserWorkItem(delegate
					{
						p.WaitForExit();
						OnExit(p.ExitCode);
						isRunning = false;
					});
				}
				else
				{
					Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown(0));
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("패치도중 오류가 발생하였습니다. 자세한 내용은 콘솔에 표시됩니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
				MainWindow.Monitor.Error(e.ToString());
			}
		}

		private static void OnError(string str)
		{
			MainWindow.Monitor.Error(str);
		}

		private static void OnOutput(string str)
		{
			MainWindow.Monitor.Info(str);
		}

		private void OnExit(int exitcode)
		{
			MainWindow.Monitor.Error("# 마인크래프트가 종료되었습니다. (" + exitcode + ")");
			Exit?.Invoke(this, null);
		}
	}
}
