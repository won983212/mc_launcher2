using Microsoft.WindowsAPICodePack.Dialogs;
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
		private volatile bool connecting = false;

		public MainWindow()
		{
			InitializeComponent();
			warnings = new WarningManager(this);

			CheckMinecraftFolder();
			LoadInfoFromServer();
		}

		public static ConsoleIO Monitor { get; private set; } = new ConsoleWindow();

		private void CheckMinecraftFolder()
		{
			if(!Directory.Exists(settings.Minecraft_Dir))
			{
				warnings.ShowWarning(WarningManager.NeedInstall);
			}
		}

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
				pnlConnectionState.Visibility = Visibility.Collapsed;
				chbUpdate.IsEnabled = true;

				if (settings.ClientVersion != latest)
				{
					warnings.ShowWarning(WarningManager.Outdated);
					chbUpdate.IsChecked = true;
				}
			}
			else
			{
				pnlConnectionState.Visibility = Visibility.Collapsed;
				warnings.ShowWarning(WarningManager.ConnectionError);
			}
			connecting = false;
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if(e.ChangedButton == MouseButton.Left)
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
	}

	internal class TimeoutWebClient : WebClient
	{
		public int Timeout { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest w = base.GetWebRequest(address);
			w.Timeout = Timeout;
			((HttpWebRequest)w).ReadWriteTimeout = Timeout;
			return w;
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

		public void ShowWarning(int id)
		{
			if (messages == 0)
				wnd.pnlWarnings.Visibility = Visibility.Visible;
			if ((messages & id) != 0)
				return;
			actualElements[id].Visibility = Visibility.Visible;
			messages += id;
		}

		public void HideWarning(int id)
		{
			if ((messages & id) == 0)
				return;
			actualElements[id].Visibility = Visibility.Collapsed;
			messages -= id;
			if (messages == 0)
				wnd.pnlWarnings.Visibility = Visibility.Collapsed;
		}
	}
}
