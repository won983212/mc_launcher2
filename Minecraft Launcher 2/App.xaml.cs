using Minecraft_Launcher_2.Pages.ViewModels.Dialogs;
using Minecraft_Launcher_2.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace Minecraft_Launcher_2
{
    public partial class App : Application
    {
        public static ConsoleWindow Console { get; private set; }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var vmodel = Current.MainWindow.DataContext as MainViewModel;
            if (vmodel != null)
            {
                CommonUtils.ShowDialog(new ErrorDialogVM(e.Exception));
                e.Handled = true;
            }

            Logger.Error(e.Exception);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Console = new ConsoleWindow();
        }
    }
}