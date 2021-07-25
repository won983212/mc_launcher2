using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Controls
{
    public partial class ProgressStatusBar : UserControl
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(ProgressStatus), typeof(ProgressStatusBar));

        public ProgressStatusBar()
        {
            InitializeComponent();
        }

        public ProgressStatus Progress
        {
            get => (ProgressStatus) GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }
    }

    public class ProgressStatus : ObservableObject
    {
        private bool _isShow;
        private double _progress;
        private string _status = "";


        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsShow
        {
            get => _isShow;
            set => SetProperty(ref _isShow, value);
        }


        public void SetProgress(string status, double progress)
        {
            Status = status;
            Progress = progress;
        }
    }
}