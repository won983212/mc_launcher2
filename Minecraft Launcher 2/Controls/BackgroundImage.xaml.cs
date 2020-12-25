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
    /// BackgroundImage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BackgroundImage : UserControl
    {
        public const int CountPhoto = 3;
        private static int[] _photoOrder = new int[CountPhoto];
        private static int _photoIdx = 0;

        public BackgroundImage()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new DispatcherTimer();
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

            DoubleAnimation animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            imgTransition.BeginAnimation(OpacityProperty, animation);
        }

        private void SetBackground(Image img, int idx)
        {
            img.Source = new BitmapImage(new Uri("../Resources/background" + _photoOrder[idx] + ".png", UriKind.Relative));
        }

        static BackgroundImage()
        {
            Random r = new Random();
            for(int i = 0; i < CountPhoto; i++)
                _photoOrder[i] = i;
            for (int i = 0; i < CountPhoto - 1; i++)
            {
                int j = r.Next(i, CountPhoto);
                int t = _photoOrder[j];
                _photoOrder[j] = _photoOrder[i];
                _photoOrder[i] = t;
            }
        }
    }
}
