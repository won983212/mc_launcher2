using MaterialDesignThemes.Wpf;
using Minecraft_Launcher_2.Dialogs.ViewModels;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;
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

        public ServerInfo(ServerStatus status)
        {
            Motd = status.Motd;
            PlayerCount = status.PlayersOnline + "/" + status.PlayersMax;
            Ping = status.Ping + "ms";
        }
    }

    internal class MainViewModel : ObservableObject, IForceUpdateContoller
    {
        private string _signalIcon = "SignalCellular1";
        private Visibility _signalIconVisibility = Visibility.Collapsed;

        private string _welcomeMessage = "Loading...";
        private ServerInfo _serverInfo;

        private readonly LauncherContext _context;
        private readonly MinecraftLauncher _launcher;
        private string _startText = "연결 중..";
        private LauncherState _launchState;
        private bool _canStart = false;

        // TODO Download 부분은 나눠서 만들기
        private bool _isShowDownloadStatus = false;
        private double _downloadProgress = 0;
        private string _downloadStatus = "";


        public MainViewModel()
        {
            _context = new LauncherContext();
            _launcher = new MinecraftLauncher(_context);
            SnackMessages = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));

            ServerStatus status = _context.ServerStatus;
            status.OnConnectionStateChanged += ServerStatus_OnConnectionStateChanged;
            if (status.ConnectionState.State != RetrieveState.Processing)
                ServerStatus_OnConnectionStateChanged(null, status.ConnectionState);

            _launcher.OnLog += (s, t) => Logger.Log(t);
            _launcher.OnError += (s, t) => Logger.Error(t);
            _launcher.OnExited += (s, t) => Logger.Log(" Exited (code: " + t + ")");

            _context.GetInstalledPatchVersion();
            status.RetrieveAll();
        }


        public async void StartDownload()
        {
            IsShowDownloadStatus = true;
            _canStart = false;
            DownloadStatus = "다운로드 중..";
            DownloadProgress = 0;

            ContentUpdater updater = new ContentUpdater();
            updater.OnProgress += Updater_OnProgress;
            int failed = await updater.BeginDownload();

            IsShowDownloadStatus = false;
            _context.GetInstalledPatchVersion();
            UpdateStartButton();

            if (failed > 0)
            {
                MessageBoxResult res = MessageBox.Show("파일 " + failed + "개를 받지 못했습니다. 그래도 실행합니까?", "주의", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    StartMinecraft();
                }
                else
                {
                    _canStart = true;
                }
            }
            else
            {
                StartMinecraft();
            }
        }

        public async Task StartMinecraft()
        {
            Settings settings = Settings.Default;
            _launcher.PlayerName = settings.PlayerName;
            settings.Save();

            await _launcher.Start();
            _canStart = true;

            if (!Settings.Default.UseLogging)
                App.Current.Shutdown(0);
        }

        public void SetForceUpdate()
        {
            if (ConnectionState == RetrieveState.Loaded)
            {
                StartText = "업데이트";
                _launchState = LauncherState.NeedUpdate;
            }
        }

        private void Updater_OnProgress(object sender, ProgressArgs e)
        {
            DownloadStatus = e.Status;
            DownloadProgress = e.Progress;
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
            {
                StartMinecraft();
            }
            else
            {
                StartDownload();
            }
        }

        private bool CanStart(object parameter)
        {
            return _canStart && !_launcher.IsRunning;
        }

        private void ServerStatus_OnConnectionStateChanged(object sender, ConnectionState e)
        {
            if (e.State == RetrieveState.Loaded)
            {
                SignalIconVisibility = Visibility.Visible;
                ServerStatus status = _context.ServerStatus;

                SignalIcon = "SignalCellular1";
                if (status.Ping < 150)
                {
                    SignalIcon = "SignalCellular3";
                }
                else if (status.Ping < 300)
                {
                    SignalIcon = "SignalCellular2";
                }

                ServerInfo = new ServerInfo(status);
                WelcomeMessage = status.Notice;
            }
            else
            {
                SignalIconVisibility = Visibility.Collapsed;
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
            set
            {
                _welcomeMessage = value;
                OnPropertyChanged();
            }
        }

        public string SignalIcon
        {
            get => _signalIcon;
            set
            {
                _signalIcon = value;
                OnPropertyChanged();
            }
        }

        public Visibility SignalIconVisibility
        {
            get => _signalIconVisibility;
            set
            {
                _signalIconVisibility = value;
                OnPropertyChanged();
            }
        }

        public ServerInfo ServerInfo
        {
            get => _serverInfo;
            set
            {
                _serverInfo = value;
                OnPropertyChanged();
            }
        }

        public bool IsShowDownloadStatus
        {
            get => _isShowDownloadStatus;
            private set
            {
                _isShowDownloadStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsFormEnabled
        {
            get => !IsShowDownloadStatus;
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            private set
            {
                _downloadProgress = value;
                OnPropertyChanged();
            }
        }

        public string DownloadStatus
        {
            get => _downloadStatus;
            private set
            {
                _downloadStatus = value;
                OnPropertyChanged();
            }
        }

        public RetrieveState ConnectionState
        {
            get => _context.ServerStatus.ConnectionState.State;
        }

        public string ConnectionErrorMessage
        {
            get
            {
                string message = _context.ServerStatus.ConnectionState.ErrorMessage;
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
            set
            {
                _startText = value;
                OnPropertyChanged();
            }
        }


        public ICommand ReconnectCommand => new RelayCommand(() => _context.ServerStatus.RetrieveAll());

        public ICommand ShowSettingCommand => new RelayCommand(() => CommonUtils.ShowDialog(new SettingDialogVM(this)));

        public ICommand StartCommand => new RelayCommand(OnStartClick, CanStart);

        public ICommand ShowConsoleCommand => new RelayCommand(() =>
        {
            if (App.Console != null)
                App.Console.Show();
        });
    }
}
