using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Minecraft_Launcher_2.Controls
{
    /// <summary>
    /// ErrorDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ErrorDialog : UserControl
    {
        public static readonly DependencyProperty ErrorObjectProperty = DependencyProperty.Register("ErrorObject",
            typeof(ErrorMessageObject), typeof(ErrorDialog), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnErrorObjectChanged)));

        public static readonly DependencyProperty IsShowProperty = DependencyProperty.Register("IsShow", 
            typeof(bool), typeof(ErrorDialog), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsShowChanged)));

        public ErrorMessageObject ErrorObject
        {
            get { return (ErrorMessageObject)GetValue(ErrorObjectProperty); }
            set { SetValue(ErrorObjectProperty, value); }
        }

        public bool IsShow
        {
            get { return (bool)GetValue(IsShowProperty); }
            set { SetValue(IsShowProperty, value); }
        }

        private static readonly TimeSpan AnimationTimeSpan = TimeSpan.FromSeconds(0.1);
        private DoubleAnimation _animation;
        private DoubleAnimation _animationRev;
        private Action _callback;

        public ErrorDialog()
        {
            _animation = new DoubleAnimation(0, 1, AnimationTimeSpan);
            _animation.EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut };
            _animationRev = new DoubleAnimation(1, 0, AnimationTimeSpan);
            _animationRev.EasingFunction = _animation.EasingFunction;

            Visibility = Visibility.Collapsed;
            InitializeComponent();
        }

        private void ShowMessage()
        {
            Visibility = Visibility.Visible;
            rectBg.BeginAnimation(OpacityProperty, _animation);
            scalePanel.BeginAnimation(ScaleTransform.ScaleXProperty, _animation);
            scalePanel.BeginAnimation(ScaleTransform.ScaleYProperty, _animation);
            IsShow = true;
        }

        private void CloseMessage()
        {
            _callback?.Invoke();
            rectBg.BeginAnimation(OpacityProperty, _animationRev);
            scalePanel.BeginAnimation(ScaleTransform.ScaleXProperty, _animationRev);
            scalePanel.BeginAnimation(ScaleTransform.ScaleYProperty, _animationRev);

            var timer = new DispatcherTimer { Interval = AnimationTimeSpan };
            timer.Tick += (s, args) =>
            {
                Visibility = Visibility.Collapsed;
                IsShow = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            CloseMessage();
        }

        public static void OnErrorObjectChanged(DependencyObject obj, DependencyPropertyChangedEventArgs arg)
        {
            ErrorDialog dialog = obj as ErrorDialog;
            ErrorMessageObject message = arg.NewValue as ErrorMessageObject;

            dialog.tbTitle.Text = message.Title;
            dialog.tbMessage.Text = message.Message;
            dialog._callback = message.Callback;

            if (message.FullMessage != null)
                dialog.btnDetail.Visibility = Visibility.Visible;
            else
                dialog.btnDetail.Visibility = Visibility.Collapsed;
        }

        public static void OnIsShowChanged(DependencyObject obj, DependencyPropertyChangedEventArgs arg)
        {
            ErrorDialog dialog = obj as ErrorDialog;
            if ((bool)arg.NewValue)
                dialog.ShowMessage();
            else
                dialog.CloseMessage();
        }

        private void ShowFullMessage_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(ErrorObject.FullMessage, "이 내용을 복사하시겠습니까?", MessageBoxButton.YesNo);
            if(res == MessageBoxResult.Yes)
            {
                Clipboard.SetText(ErrorObject.FullMessage);
            }
        }
    }
}
