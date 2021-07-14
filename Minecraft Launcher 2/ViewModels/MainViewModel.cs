using MaterialDesignThemes.Wpf;
using Minecraft_Launcher_2.Dialogs.ViewModels;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.ViewModels
{
    internal class ServerInfo
    {
        public string Motd { get; set; }
        public string PlayerCount { get; set; }
        public string Ping { get; private set; }

        public ServerInfo(ServerInfoRetriever status)
        {
            Motd = status.MinecraftServerData.Motd;
            PlayerCount = status.MinecraftServerData.PlayersOnline + "/" + status.MinecraftServerData.PlayersMax;
            Ping = status.MinecraftServerData.Ping + "ms";
        }
    }

    internal class MainViewModel : ObservableObject, IForceUpdateContoller
    {
        private readonly LauncherContext _context;

        private string _signalIcon = "";
        private string _welcomeMessage = "Loading...";
        private ServerInfo _serverInfo;
        private string _startText = "연결 중..";
        private LauncherState _launchState;
        private bool _canStart = false;


        public MainViewModel()
        {
            _context = new LauncherContext();
            Updater = new UpdaterViewModel(_context);
            SnackMessages = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));

            ServerInfoRetriever status = _context.Retriever;
            status.OnConnectionStateChanged += ServerStatus_OnConnectionStateChanged;
            if (status.ConnectionState.State != RetrieveState.Processing)
                ServerStatus_OnConnectionStateChanged(null, status.ConnectionState);

            _context.GetInstalledPatchVersion();
            status.RetrieveAll();
        }

        public void SetForceUpdate()
        {
            if (ConnectionState == RetrieveState.Loaded)
            {
                StartText = "업데이트";
                _launchState = LauncherState.NeedUpdate;
            }
        }

        private void UpdateStartButton()
        {
            _launchState = _context.GetLauncherState();
            switch (_launchState)
            {
                case LauncherState.CanStart:
                    StartText = "시작";
                    break;
                case LauncherState.NeedInstall:
                    StartText = "설치";
                    break;
                case LauncherState.NeedUpdate:
                    StartText = "업데이트";
                    break;
                case LauncherState.Offline:
                    StartText = "오프라인 시작";
                    break;
                default:
                    break;
            }
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

            if (_launchState == LauncherState.Offline || _launchState == LauncherState.CanStart)
                Updater.StartMinecraft();
            else
                Updater.StartDownload().ContinueWith((t) => UpdateStartButton());
        }

        private bool CanStart(object parameter)
        {
            return _canStart && !Updater.IsRunning;
        }

        private void ServerStatus_OnConnectionStateChanged(object sender, ConnectionState e)
        {
            if (e.State == RetrieveState.Loaded)
            {
                ServerInfoRetriever status = _context.Retriever;

                SignalIcon = "SignalCellular1";
                if (status.MinecraftServerData.Ping < 150)
                {
                    SignalIcon = "SignalCellular3";
                }
                else if (status.MinecraftServerData.Ping < 300)
                {
                    SignalIcon = "SignalCellular2";
                }

                ServerInfo = new ServerInfo(status);
                WelcomeMessage = status.ResourceServerData.Notice;
            }
            else
            {
                SignalIcon = "";
            }

            OnPropertyChanged("ConnectionErrorMessage");
            OnPropertyChanged("ConnectionState");
            UpdateStartButton();

            _canStart = e.State != RetrieveState.Processing;
            Application.Current.Dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
        }

        public void ShowErrorMessage(Exception e, Action callback)
        {
            CommonUtils.ShowDialog(new ErrorDialogVM(e), (vm, args) => callback());
        }

        public void ShowErrorMessage(string title, string message, Action callback)
        {
            CommonUtils.ShowDialog(new ErrorDialogVM(title, message), (vm, args) => callback());
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

        public RetrieveState ConnectionState
        {
            get => _context.Retriever.ConnectionState.State;
        }

        public string ConnectionErrorMessage
        {
            get
            {
                string message = _context.Retriever.ConnectionState.ErrorMessage;
                if (string.IsNullOrEmpty(message))
                {
                    return "연결 성공";
                }
                else
                {
                    return message;
                }
            }
        }

        public string StartText
        {
            get => _startText;
            set => SetProperty(ref _startText, value);
        }


        public ICommand ReconnectCommand => new RelayCommand(() => _context.Retriever.RetrieveAll());

        public ICommand ShowSettingCommand => new RelayCommand(() => CommonUtils.ShowDialog(new SettingDialogVM(this)));

        public ICommand StartCommand => new RelayCommand(OnStartClick, CanStart);

        public ICommand ShowConsoleCommand => new RelayCommand(() =>
        {
            if(App.Console != null)
                App.Console.Show();
        });
    }
}
