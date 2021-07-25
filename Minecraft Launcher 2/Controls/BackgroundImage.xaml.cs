using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Minecraft_Launcher_2.Controls
{
    public partial class BackgroundImage : UserControl
    {
        public const int CountPhoto = 5;
        private static readonly int[] _photoOrder = new int[CountPhoto];
        private static int _photoIdx;

        static BackgroundImage()
        {
            var r = new Random();
            for (var i = 0; i < CountPhoto; i++)
                _photoOrder[i] = i;
            for (var i = 0; i < CountPhoto - 1; i++)
            {
                var j = r.Next(i, CountPhoto);
                var t = _photoOrder[j];
                _photoOrder[j] = _photoOrder[i];
                _photoOrder[i] = t;
            }
        }

        public BackgroundImage()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Start();
            SetBackground(imgBg, _photoIdx);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            SetBackground(imgTransition, _photoIdx);
            _photoIdx = (_photoIdx + 1) % CountPhoto;
            SetBackground(imgBg, _photoIdx);

            var animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            imgTransition.BeginAnimation(OpacityProperty, animation);
        }

        private void SetBackground(Image img, int idx)
        {
            img.Source =
                new BitmapImage(new Uri("../Resources/background" + _photoOrder[idx] + ".png", UriKind.Relative));
        }
    }
}