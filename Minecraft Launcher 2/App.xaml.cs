using Minecraft_Launcher_2.ViewModels;
using System.Windows;

namespace Minecraft_Launcher_2
{
    public partial class App : Application
    {
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
