using Microsoft.WindowsAPICodePack.Dialogs;
using Minecraft_Launcher_2.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Minecraft_Launcher_2
{
	public partial class MainWindow : Window
	{
		private Properties.Settings settings = Properties.Settings.Default;
		private WarningManager warnings;
		private Launcher launcher;
		private volatile bool connecting = false;

		public MainWindow()
		{
			InitializeComponent();
			warnings = new WarningManager(this);
			launcher = new Launcher();
			launcher.Exit += Launcher_Exit;

			if (!(LauncherProfile.ClientProfile.Load() && Directory.Exists(settings.Minecraft_Dir))) // launcher_setting이 없거나 마크 폴더가 없으면 오류
				warnings.ShowWarning(WarningManager.NeedInstall);

			if (!Directory.Exists(System.IO.Path.Combine(settings.Minecraft_Dir, "assets"))) // assets가 없으면 오류
				warnings.ShowWarning(WarningManager.NeedInstall);

			if (!Directory.Exists(System.IO.Path.Combine(settings.Minecraft_Dir, "libraries"))) // libraries가 없으면 오류
				warnings.ShowWarning(WarningManager.NeedInstall);

			if (!File.Exists(System.IO.Path.Combine(settings.Minecraft_Dir, "minecraft.jar"))) // minecraft.jar가 없으면 오류
				warnings.ShowWarning(WarningManager.NeedInstall);

			lblInstalledVersion.Content = LauncherProfile.ClientProfile.ClientVersion;
			LoadInfoFromServer();
		}

		public static ConsoleIO Monitor { get; private set; } = new ConsoleWindow();
		public string LatestPatchVersion { get; private set; } = null;

		private void LoadInfoFromServer()
		{
			if (connecting)
				return;

			pnlConnectionState.Visibility = Visibility.Visible;
			warnings.HideWarning(WarningManager.ConnectionError);
			connecting = true;

			using (TimeoutWebClient client = new TimeoutWebClient())
			{
				client.Timeout = 5000;
				client.DownloadStringCompleted += OnLoadInfoCompleted;
				client.DownloadStringAsync(new Uri(settings.UpdateHost + "/version.txt"));
			}
		}

		private void OnLoadInfoCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			if (e.Error == null)
			{
				string[] parsed = e.Result.Split('\n');
				string latest = parsed[0].Trim() + "#" + parsed[1].Trim();
				LatestPatchVersion = parsed[1].Trim();
				Monitor.Info("Retrieved version from server: " + latest);

				pnlConnectionState.Visibility = Visibility.Collapsed;

				if (LauncherProfile.ClientProfile.ClientVersion != latest)
					warnings.ShowWarning(WarningManager.Outdated);
			}
			else
			{
				pnlConnectionState.Visibility = Visibility.Collapsed;
				warnings.ShowWarning(WarningManager.ConnectionError);
			}
			connecting = false;
		}

		private void StartUpdate()
		{
			pnlLogin.IsEnabled = false;
			pnlSettingInner.IsEnabled = false;
			btnGameClear.IsEnabled = false;
			btnSettingReset.IsEnabled = false;
			pnlUpdateState.Visibility = Visibility.Visible;

			MinecraftPatcher updater = new MinecraftPatcher(LatestPatchVersion);
			updater.OnException += Updater_OnException;
			updater.CurrentDownloadProgress += Updater_CurrentDownloadProgress;
			updater.TotalDownloadProgress += Updater_TotalDownloadProgress;
			updater.OnComplete += Updater_OnComplete;
			updater.UpdateAsync();
		}

		private void Updater_OnComplete(object sender, int e)
		{
			Dispatcher.Invoke(() =>
			{
				pnlUpdateState.Visibility = Visibility.Collapsed;
				pnlLogin.IsEnabled = true;
				pnlSettingInner.IsEnabled = true;
				btnGameClear.IsEnabled = true;
				btnSettingReset.IsEnabled = true;

				lblInstalledVersion.Content = LauncherProfile.ClientProfile.ClientVersion;
				warnings.HideWarning(WarningManager.NeedInstall);
				warnings.HideWarning(WarningManager.Outdated);
			});
			launcher.Start();
		}

		private void Updater_TotalDownloadProgress(object sender, Tuple<double, string> e)
		{
			Dispatcher.Invoke(() =>
			{
				tblTotalProgress.Text = "전체: " + (int)e.Item1 + "%";
				tblTotalState.Text = e.Item2;
				prgTotal.Value = e.Item1;
			});
		}

		private void Updater_CurrentDownloadProgress(object sender, Tuple<double, string> e)
		{
			Dispatcher.Invoke(() =>
			{
				tblCurrentProgress.Text = "현재: " + (int)e.Item1 + "%";
				tblCurrentState.Text = e.Item2;
				prgCurrent.Value = e.Item1;
			});
		}

		private void Updater_OnException(object sender, Exception e)
		{
			Monitor.Error(e.ToString());
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void SettingSave_Click(object sender, RoutedEventArgs e)
		{
			pnlSetting.Visibility = Visibility.Collapsed;
			Properties.Settings.Default.Save();
		}

		private void SettingOpen_Click(object sender, RoutedEventArgs e)
		{
			pnlSetting.Visibility = Visibility.Visible;
		}

		private void SettingClear_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.Reset();
			Properties.Settings.Default.Save();
			MessageBox.Show("리셋되었습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ClearAll_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult res = MessageBox.Show("컴퓨터에 설치된 마인크래프트가 완전히 제거됩니다. 작업을 취소할 수 없으며 게임뿐만 아니라 설정파일, 스크린샷, 맵 등 사용자파일도 전부 삭제됩니다. 계속하시겠습니까?", "리셋 진행", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (res == MessageBoxResult.Yes)
			{
				DeleteProgressWindow del = new DeleteProgressWindow();
				del.Left = Left;
				del.Top = Top + Height - 68;
				del.ShowDialog();
				LauncherProfile.ClientProfile.Reset();
				lblInstalledVersion.Content = LauncherProfile.ClientProfile.ClientVersion;

				warnings.ShowWarning(WarningManager.NeedInstall);
				warnings.ShowWarning(WarningManager.Outdated);
			}
		}

		private void OpenConsole_Click(object sender, RoutedEventArgs e)
		{
			((ConsoleWindow)Monitor).Show();
		}

		private void FindFolder_Click(object sender, RoutedEventArgs e)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog();
			dialog.InitialDirectory = "C:/";
			dialog.IsFolderPicker = true;
			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
				Properties.Settings.Default.Minecraft_Dir = dialog.FileName;
		}

		private void ConnectionErrorWarning_MouseDown(object sender, MouseButtonEventArgs e)
		{
			LoadInfoFromServer();
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			if (launcher.IsRunning())
				MessageBox.Show("이미 게임이 실행중입니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
			else
			{
				btnStart.IsEnabled = false;
				if (warnings.HasWarning(WarningManager.NeedInstall) || warnings.HasWarning(WarningManager.Outdated))
					StartUpdate();
				else
					launcher.Start();
			}
		}

		private void Launcher_Exit(object sender, EventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				btnStart.IsEnabled = true;
			});
		}
	}

	internal class WarningManager
	{
		public const int ConnectionError = 0x1;
		public const int Outdated = 0x2;
		public const int NeedInstall = 0x4;

		private Dictionary<int, FrameworkElement> actualElements = new Dictionary<int, FrameworkElement>();
		private int messages = 0;
		private MainWindow wnd;

		public WarningManager(MainWindow wnd)
		{
			this.wnd = wnd;
			actualElements.Add(ConnectionError, wnd.wrnConnectionError);
			actualElements.Add(Outdated, wnd.wrnOutdated);
			actualElements.Add(NeedInstall, wnd.wrnNeedInstall);
		}

		public bool HasWarning(int id)
		{
			return (messages & id) != 0;
		}

		public void ShowWarning(int id)
		{
			if (messages == 0)
				wnd.pnlWarnings.Visibility = Visibility.Visible;
			if (HasWarning(id))
				return;
			actualElements[id].Visibility = Visibility.Visible;
			messages += id;
		}

		public void HideWarning(int id)
		{
			if (!HasWarning(id))
				return;
			actualElements[id].Visibility = Visibility.Collapsed;
			messages -= id;
			if (messages == 0)
				wnd.pnlWarnings.Visibility = Visibility.Collapsed;
		}
	}
}
