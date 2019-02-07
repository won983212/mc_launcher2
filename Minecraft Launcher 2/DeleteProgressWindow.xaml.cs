using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;

namespace Minecraft_Launcher_2
{
	/// <summary>
	/// DeleteProgressWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class DeleteProgressWindow : Window
	{
		public DeleteProgressWindow()
		{
			InitializeComponent();
			SetState(0, "삭제 준비 중...");
			Task.Factory.StartNew(DeleteFolder);
		}

		private void SetState(double percent, string state)
		{
			Dispatcher.Invoke(() => 
			{
				prgCurrent.Value = percent;
				tblCurrent.Text = percent + "%";
				tblState.Text = state;
			});
		}

		private void DeleteFolder()
		{
			try
			{
				string basePath = Properties.Settings.Default.Minecraft_Dir;
				if (!Directory.Exists(basePath))
				{
					Dispatcher.Invoke(() => Close());
					return;
				}

				int deleted = 0;
				string[] directories = Directory.GetDirectories(basePath);
				string[] files = Directory.GetFiles(basePath);
				double total = directories.Length + files.Length;

				foreach (string path in directories)
				{
					SetState((int)(deleted++ / total * 100), "폴더 " + System.IO.Path.GetFileName(path) + " 삭제 중..");
					Directory.Delete(path, true);
				}

				foreach (string path in files)
				{
					SetState((int)(deleted++ / total * 100), System.IO.Path.GetFileName(path) + " 삭제 중..");
					File.Delete(path);
				}

				Directory.Delete(basePath);
				Dispatcher.Invoke(() => Close());
				MessageBox.Show("완전히 삭제되었습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception e)
			{
				MainWindow.Monitor.Error(e.ToString());
				Dispatcher.Invoke(() => Close());
				MessageBox.Show("삭제할 수 없습니다. 자세한 내용은 콘솔에 표시됩니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}
	}
}
