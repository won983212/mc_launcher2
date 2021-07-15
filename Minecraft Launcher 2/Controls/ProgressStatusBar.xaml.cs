using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Controls
{
    public partial class ProgressStatusBar : UserControl
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(ProgressStatus), typeof(ProgressStatusBar));

        public ProgressStatus Progress
        {
            get => (ProgressStatus)GetValue(ProgressProperty);
            set { SetValue(ProgressProperty, value); }
        }

        public ProgressStatusBar()
        {
            InitializeComponent();
        }
    }

    public class ProgressStatus : ObservableObject
    {
        private bool _isShow = false;
        private string _status = "";
        private double _progress = 0;


        public void SetProgress(string status, double progress)
        {
            Status = status;
            Progress = progress;
        }


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
    }
}
