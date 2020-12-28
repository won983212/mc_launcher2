using Minecraft_Launcher_2.Updater;
using Minecraft_Launcher_2.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_Launcher_2
{
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application
	{
		public static LauncherContext MainContext { get; private set; } = new LauncherContext();

		public static ConsoleWindow Console { get; private set; }

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MainViewModel vmodel = Current.MainWindow.DataContext as MainViewModel;
			vmodel.ShowErrorMessage(e.Exception, () => { });
			e.Handled = true;
		}

        private void Application_Startup(object sender, StartupEventArgs e)
        {
			Console = new ConsoleWindow();
		}
    }
}
