﻿using MaterialDesignThemes.Wpf;
using Minecraft_Launcher_2.Updater;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.ViewModels
{
    class ServerInfo
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

    class MainViewModel : ObservableObject
    {
        private ErrorMessageObject _errorInfo;
        private string _signalIcon = "SignalCellular1";
        private Visibility _signalIconVisibility = Visibility.Collapsed;
        private bool _showErrorDialog = false;
        private bool _showControlPanel = false;
        private string _motd = "Loading...";
        private ServerInfo _serverInfo;

        private Launcher _launcher = new Launcher();
        private string _startText = "연결 중..";
        private LauncherState _launchState;
        private bool _canStart = false;

        private bool _isShowDownloadStatus = false;
        private double _downloadProgress = 0;
        private string _downloadStatus = "";

        private ICommand _reconnectCommand;
        private ICommand _showSettingCommand;
        private ICommand _startCommand;

        public SnackbarMessageQueue SnackMessages { get; private set; }

        public ErrorMessageObject ErrorInfo
        {
            get => _errorInfo;
            set
            {
                _errorInfo = value;
                OnPropertyChanged();
            }
        }

        public string Motd
        {
            get => _motd;
            set
            {
                _motd = value;
                OnPropertyChanged();
            }
        }

        public bool ShowErrorDialog
        {
            get => _showErrorDialog;
            set
            {
                _showErrorDialog = value;
                OnPropertyChanged();
            }
        }

        public bool ShowControlPanel
        {
            get => _showControlPanel;
            set
            {
                _showControlPanel = value;
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
            get => App.MainContext.ServerStatus.ConnectionState.State;
        }

        public string ConnectionErrorMessage
        {
            get
            {
                string message = App.MainContext.ServerStatus.ConnectionState.ErrorMessage;
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

        public ICommand ReconnectCommand
        {
            get => _reconnectCommand = _reconnectCommand ?? new RelayCommand((a) => App.MainContext.ServerStatus.RetrieveAll());
        }

        public ICommand ShowSettingCommand
        {
            get => _showSettingCommand = _showSettingCommand ?? new RelayCommand((a) => ShowControlPanel = true);
        }

        public ICommand StartCommand
        {
            get
            {
                if (_startCommand == null)
                    _startCommand = new RelayCommand(OnStartClick, CanStart);
                return _startCommand;
            }
        }

        public MainViewModel()
        {
            SnackMessages = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));
            ErrorInfo = new ErrorMessageObject();
            App.MainContext.ServerStatus.OnConnectionStateChanged += ServerStatus_OnConnectionStateChanged;

            _launcher.OnLog += (s, t) => Logger.Log(t);
            _launcher.OnError += (s, t) => Logger.Error(t);
            _launcher.OnExited += (s, t) => Logger.Log(" Exited (code: " + t + ")");
        }

        public void StartDownload()
        {
            IsShowDownloadStatus = true;
            _canStart = false;
            DownloadStatus = "다운로드 중..";
            DownloadProgress = 0;

            ContentUpdater updater = new ContentUpdater();
            updater.OnProgress += Updater_OnProgress;
            updater.BeginDownload();
        }

        public async Task StartMinecraft()
        {
            _launcher.PlayerName = Properties.Settings.Default.PlayerName;
            Properties.Settings.Default.Save();

            await _launcher.Start();
            _canStart = true;

            if (!Properties.Settings.Default.UseLogging)
                App.Current.Shutdown(0);
        }

        private void Updater_OnProgress(object sender, ProgressArgs e)
        {
            DownloadStatus = e.Status;
            DownloadProgress = e.Progress;

            if (IsShowDownloadStatus && e.Progress >= 100)
            {
                IsShowDownloadStatus = false;
                App.MainContext.UpdatePatchVersion();
                UpdateStartButton();
                StartMinecraft();
            }
        }

        private void UpdateStartButton()
        {
            _launchState = App.MainContext.GetLauncherState();
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
            }
        }

        private bool IsLegalUsername(string name)
        {
            foreach (char c in name)
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".IndexOf(c) == -1)
                    return false;
            return true;
        }

        private void OnStartClick(object arg)
        {
            string playerName = Properties.Settings.Default.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                AddErrorSnackbar("닉네임을 입력해주세요.");
                return;
            }
            else if (!IsLegalUsername(playerName))
            {
                AddErrorSnackbar("닉네임은 영문, 숫자, 언더바(_)로만 구성해야합니다.");
                return;
            }

            if (_launchState != LauncherState.CanStart)
            {
                StartDownload();
            }
            else
            {
                StartMinecraft();
            }
        }

        private bool CanStart(object arg)
        {
            return _canStart && !_launcher.IsRunning;
        }

        private void ServerStatus_OnConnectionStateChanged(object sender, ConnectionState e)
        {
            if (e.State == RetrieveState.Loaded)
            {
                SignalIconVisibility = Visibility.Visible;
                ServerStatus status = App.MainContext.ServerStatus;

                if (status.Ping < 150)
                {
                    SignalIcon = "SignalCellular3";
                }
                else if (status.Ping < 300)
                {
                    SignalIcon = "SignalCellular2";
                }
                else
                {
                    SignalIcon = "SignalCellular1";
                }

                ServerInfo = new ServerInfo(status);
                Motd = status.Notice;
            }
            else
            {
                SignalIconVisibility = Visibility.Collapsed;
            }

            OnPropertyChanged("ConnectionErrorMessage");
            OnPropertyChanged("ConnectionState");
            UpdateStartButton();
            _canStart = true;
        }

        public void ShowErrorMessage(Exception e, Action callback)
        {
            ErrorInfo = new ErrorMessageObject()
            {
                Title = "오류 발생",
                Message = e.Message,
                FullMessage = e.ToString(),
                Callback = callback
            };
            ShowErrorDialog = true;
        }

        public void ShowErrorMessage(string title, string message, Action callback)
        {
            ErrorInfo = new ErrorMessageObject()
            {
                Title = title,
                Message = message,
                FullMessage = null,
                Callback = callback
            };
            ShowErrorDialog = true;
        }

        public void AddErrorSnackbar(string message)
        {
            SnackMessages.Enqueue(message);
        }
    }
}
