using MaterialDesignThemes.Wpf;
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
    class MainViewModel : ObservableObject
    {
        private ErrorMessageObject _errorInfo;
        private string _signalIcon = "SignalCellular1";
        private Visibility _signalIconVisibility = Visibility.Collapsed;
        private bool _showErrorDialog = false;
        private bool _showControlPanel = false;

        private ICommand _reconnectCommand;
        private ICommand _showSettingCommand;

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

        public ICommand ReconnectCommand
        {
            get => _reconnectCommand = _reconnectCommand ?? new RelayCommand((a) => App.MainContext.ServerStatus.RetrieveAll());
        }

        public ICommand ShowSettingCommand
        {
            get => _showSettingCommand = _showSettingCommand ?? new RelayCommand((a) => ShowControlPanel = true);
        }

        public MainViewModel()
        {
            SnackMessages = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));
            ErrorInfo = new ErrorMessageObject();
            App.MainContext.ServerStatus.OnConnectionStateChanged += ServerStatus_OnConnectionStateChanged;
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
            }
            else
            {
                SignalIconVisibility = Visibility.Collapsed;
            }
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
