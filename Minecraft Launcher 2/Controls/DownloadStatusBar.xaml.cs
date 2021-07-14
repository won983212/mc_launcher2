using Minecraft_Launcher_2.Updater;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Minecraft_Launcher_2.Controls
{
    public partial class DownloadStatusBar : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
            typeof(UpdaterViewModel), typeof(DownloadStatusBar), new FrameworkPropertyMetadata(OnViewModelChanged));

        public UpdaterViewModel ViewModel
        {
            get => (UpdaterViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private static readonly TimeSpan AnimationTimeSpan = TimeSpan.FromSeconds(0.5);
        private readonly DoubleAnimation _animation;
        private readonly DoubleAnimation _animationRev;

        public DownloadStatusBar()
        {
            _animation = new DoubleAnimation(60, 0, AnimationTimeSpan)
            {
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
            };

            _animationRev = new DoubleAnimation(0, 60, AnimationTimeSpan)
            {
                EasingFunction = _animation.EasingFunction
            };

            _animationRev.Completed += _animationRev_Completed;
            InitializeComponent();
        }

        private void ShowMessage()
        {
            Visibility = Visibility.Visible;
            translatePanel.BeginAnimation(TranslateTransform.YProperty, _animation);
        }

        private void CloseMessage()
        {
            translatePanel.BeginAnimation(TranslateTransform.YProperty, _animationRev);
        }

        private void _animationRev_Completed(object sender, EventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UpdaterViewModel.IsShowDownloadStatus))
            {
                if (ViewModel.IsShowDownloadStatus)
                    ShowMessage();
                else
                    CloseMessage();
            }
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                DownloadStatusBar bar = (DownloadStatusBar)d;
                if (e.OldValue != null)
                    ((UpdaterViewModel)e.OldValue).PropertyChanged -= bar.ViewModel_PropertyChanged;
                ((UpdaterViewModel)e.NewValue).PropertyChanged += bar.ViewModel_PropertyChanged;
            }
        }
    }
}
