using MaterialDesignThemes.Wpf;
using Minecraft_Launcher_2.Dialogs.ViewModels;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater;
using Minecraft_Launcher_2.Updater.ServerConnections;
using System;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.ViewModels
{
    internal class ServerInfo
    {
        public string Motd { get; set; }
        public string PlayerCount { get; set; }
        public string Ping { get; private set; }

        public ServerInfo(MinecraftServerInfo status)
        {
            Motd = status.Motd;
            PlayerCount = status.PlayersOnline + "/" + status.PlayersMax;
            Ping = status.Ping + "ms";
        }
    }

    internal class MainViewModel : ObservableObject
    {
        private readonly ServerDataContext _context;
        private bool _isManagementButtonActive = false;

        private string _signalIcon = "SignalOff";
        private ServerInfo _serverInfo;

        private string _welcomeMessage = "Loading...";
        private string _startButtonText = "연결 중..";
        private LauncherState _launchState;


        public MainViewModel()
        {
            _context = new ServerDataContext();
            Updater = new UpdaterViewModel(_context);
            SnackMessages = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));

            APIServerInfoRetriever status = _context.Retriever;
            status.OnConnectionStateChanged += ServerStatus_OnConnectionStateChanged;
            if (status.ConnectionState.State != RetrieveState.Processing)
                ServerStatus_OnConnectionStateChanged(null, status.ConnectionState);

            ServerSettingPanelViewModel = new ServerSettingPanelVM();
            ServerSettingPanelViewModel.PanelClosed += (s, e) => status.RetrieveFromAPIServer();

            _context.ReadInstalledPatchVersion();
            status.RetrieveFromAPIServer();
            RetrieveMinecraftServerStatus();
        }


        private void OnStartClick()
        {
            string playerName = Settings.Default.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                AddErrorSnackbar("닉네임을 입력해주세요.");
                return;
            }
            else if (!CommonUtils.IsLegalUsername(playerName))
            {
                AddErrorSnackbar("닉네임은 영문, 숫자, 언더바(_)로만 구성해야합니다.");
                return;
            }

            bool useAutoJoin = SignalIcon != "Loading" && SignalIcon != "SignalOff" && Settings.Default.AutoJoinServer;
            if (_launchState == LauncherState.Offline || _launchState == LauncherState.CanStart)
                Updater.StartMinecraft(useAutoJoin);
            else
                Updater.StartDownload(useAutoJoin).ContinueWith((t) => UpdateStartButton(false));
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
            CommonUtils.IsActiveHttpServer(URLs.LocalInfoFile).ContinueWith((t) => IsManagementButtonActive = t.Result);
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
                default:
                    break;
            }
        }

        private async void RetrieveMinecraftServerStatus()
        {
            if (SignalIcon == "Loading")
                return;

            SignalIcon = "Loading";
            MinecraftServerConnection con = new MinecraftServerConnection();
            try
            {
                MinecraftServerInfo info = await con.RetrieveServerStatusAsync();
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

        public void AddErrorSnackbar(string message)
        {
            SnackMessages.Enqueue(message);
        }


        public SnackbarMessageQueue SnackMessages { get; private set; }

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

        public RetrieveState ConnectionState
        {
            get => _context.Retriever.ConnectionState.State;
        }

        public string ConnectionErrorMessage
        {
            get
            {
                string message = _context.Retriever.ConnectionState.ErrorMessage;
                return string.IsNullOrEmpty(message) ? "연결 성공" : message;
            }
        }

        public string StartButtonText
        {
            get => _startButtonText;
            set => SetProperty(ref _startButtonText, value);
        }

        public bool IsManagementButtonActive
        {
            get => _isManagementButtonActive;
            set => SetProperty(ref _isManagementButtonActive, value);
        }


        public ICommand ReconnectAPIServerCommand => new RelayCommand(() => _context.Retriever.RetrieveFromAPIServer());

        public ICommand ReconnectMinecraftServerCommand => new RelayCommand(RetrieveMinecraftServerStatus);

        public ICommand StartCommand => new RelayCommand(OnStartClick, CanStart);

        public ICommand OpenSettingDialogCommand => new RelayCommand(() => CommonUtils.ShowDialog(new SettingDialogVM(), (vm, a) => UpdateStartButton(vm.UseForceUpdate)));

        public ICommand OpenServerSettingPanelCommand => new RelayCommand(ServerSettingPanelViewModel.Open);

        public ICommand OpenConsoleCommand => new RelayCommand(() =>
        {
            if (App.Console != null)
                App.Console.Show();
        });
    }
}
