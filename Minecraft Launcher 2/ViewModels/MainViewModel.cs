using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Pages.Dialogs.ViewModels;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater;
using Minecraft_Launcher_2.Updater.ServerConnections;

namespace Minecraft_Launcher_2.ViewModels
{
    internal class ServerInfo
    {
        public ServerInfo(MinecraftServerInfo status)
        {
            Motd = status.Motd;
            PlayerCount = status.PlayersOnline + "/" + status.PlayersMax;
            Ping = status.Ping + "ms";
        }

        public string Motd { get; set; }
        public string PlayerCount { get; set; }
        public string Ping { get; }
    }

    internal class MainViewModel : ObservableObject
    {
        private readonly ServerDataContext _context;
        private bool _isManagementButtonActive;
        private bool _isSplashActive;
        private LauncherState _launchState;

        private string _loginErrorMessage = "";
        private ServerInfo _serverInfo;

        private string _signalIcon = "SignalOff";
        private string _startButtonText = "연결 중..";
        private string _welcomeMessage = "Loading...";


        public MainViewModel()
        {
            _context = new ServerDataContext();
            Updater = new UpdaterViewModel(_context);

            var status = _context.Retriever;
            status.OnConnectionStateChanged += ServerStatus_OnConnectionStateChanged;
            if (status.ConnectionState.State != RetrieveState.Processing)
                ServerStatus_OnConnectionStateChanged(null, status.ConnectionState);

            ServerSettingPanelViewModel = new ServerSettingPanelVM();
            ServerSettingPanelViewModel.PanelClosed += (s, e) => status.RetrieveFromAPIServer();

            _context.ReadInstalledPatchVersion();
            status.RetrieveFromAPIServer();
            RetrieveMinecraftServerStatus();
        }


        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string SignalIcon
        {
            get => _signalIcon;
            set => SetProperty(ref _signalIcon, value);
        }

        public ServerInfo ServerInfo
        {
            get => _serverInfo;
            set => SetProperty(ref _serverInfo, value);
        }

        public UpdaterViewModel Updater { get; }

        public ServerSettingPanelVM ServerSettingPanelViewModel { get; }

        public RetrieveState ConnectionState => _context.Retriever.ConnectionState.State;

        public string ConnectionErrorMessage
        {
            get
            {
                var message = _context.Retriever.ConnectionState.ErrorMessage;
                return string.IsNullOrEmpty(message) ? "연결 성공" : message;
            }
        }

        public string StartButtonText
        {
            get => _startButtonText;
            set => SetProperty(ref _startButtonText, value);
        }

        public bool IsSplashActive
        {
            get => _isSplashActive;
            set => SetProperty(ref _isSplashActive, value);
        }

        public bool IsManagementButtonActive
        {
            get => _isManagementButtonActive;
            set => SetProperty(ref _isManagementButtonActive, value);
        }

        public string LoginErrorMessage
        {
            get => _loginErrorMessage;
            set => SetProperty(ref _loginErrorMessage, value);
        }


        public ICommand ReconnectAPIServerCommand => new RelayCommand(() => _context.Retriever.RetrieveFromAPIServer());

        public ICommand ReconnectMinecraftServerCommand => new RelayCommand(RetrieveMinecraftServerStatus);

        public ICommand StartCommand => new RelayCommand(OnStartClick, CanStart);

        public ICommand OpenSettingDialogCommand => new RelayCommand(() =>
            CommonUtils.ShowDialog(new SettingDialogVM(), (vm, a) => UpdateStartButton(vm.UseForceUpdate)));

        public ICommand OpenServerSettingPanelCommand => new RelayCommand(ServerSettingPanelViewModel.Open);


        private void OnStartClick()
        {
            var playerName = Settings.Default.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                LoginErrorMessage = "닉네임을 입력해주세요.";
                return;
            }

            if (!CommonUtils.IsLegalUsername(playerName))
            {
                LoginErrorMessage = "닉네임은 영문, 숫자, 언더바(_)로만 구성해야합니다.";
                return;
            }

            LoginErrorMessage = "";
            if (_launchState == LauncherState.Offline || _launchState == LauncherState.CanStart)
            {
                GoStartingScreen();
            }
            else
            {
                Updater.StartDownload().ContinueWith(t =>
                {
                    UpdateStartButton(false);
                    GoStartingScreen();
                });
            }
        }

        private async void GoStartingScreen()
        {
            IsSplashActive = true;

            await Task.Delay(2000);
            Updater.StartMinecraft(SignalIcon != "Loading" && SignalIcon != "SignalOff" &&
                                   Settings.Default.AutoJoinServer);

            if (Settings.Default.UseLogging)
            {
                if (App.Console != null)
                    App.Console.Show();
            }

            await Task.Delay(5000);

            if (Settings.Default.UseLogging)
            {
                Application.Current.Windows.OfType<Window>().Where(wnd => wnd.Title == "Minecraft Launcher").Single()
                    .Close();
                App.Console.UseCloseShutdown = true;
                return;
            }

            Application.Current.Shutdown(0);
        }

        private bool CanStart(object parameter)
        {
            return _context.Retriever.ConnectionState.State != RetrieveState.Processing && !Updater.IsRunning;
        }

        private void ServerStatus_OnConnectionStateChanged(object sender, ConnectionState e)
        {
            if (e.State == RetrieveState.Loaded)
                WelcomeMessage = _context.Retriever.Notice;

            OnPropertyChanged(nameof(ConnectionErrorMessage));
            OnPropertyChanged(nameof(ConnectionState));
            UpdateStartButton(false);
            Application.Current.Dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
            CommonUtils.IsActiveHttpServer(URLs.LocalInfoFile).ContinueWith(t => IsManagementButtonActive = t.Result);
        }

        private void UpdateStartButton(bool useForceUpdate)
        {
            _launchState = _context.GetLauncherState();
            if (useForceUpdate && _launchState == LauncherState.CanStart)
                _launchState = LauncherState.NeedUpdate;

            switch (_launchState)
            {
                case LauncherState.CanStart:
                    StartButtonText = "시작";
                    break;
                case LauncherState.NeedInstall:
                    StartButtonText = "설치";
                    break;
                case LauncherState.NeedUpdate:
                    StartButtonText = "업데이트";
                    break;
                case LauncherState.Offline:
                    StartButtonText = "오프라인 시작";
                    break;
            }
        }

        private async void RetrieveMinecraftServerStatus()
        {
            if (SignalIcon == "Loading")
                return;

            SignalIcon = "Loading";
            var con = new MinecraftServerConnection();
            try
            {
                var info = await con.RetrieveServerStatusAsync();
                SignalIcon = "SignalCellular1";
                if (info.Ping < 150)
                {
                    SignalIcon = "SignalCellular3";
                }
                else if (info.Ping < 300)
                {
                    SignalIcon = "SignalCellular2";
                }

                ServerInfo = new ServerInfo(info);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                SignalIcon = "SignalOff";
            }
        }
    }
}