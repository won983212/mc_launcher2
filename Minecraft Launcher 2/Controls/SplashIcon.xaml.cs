using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Minecraft_Launcher_2.Controls
{
    public partial class SplashIcon : UserControl
    {
        private static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SplashIcon),
                new PropertyMetadata(false, IsActiveChanged));


        public SplashIcon()
        {
            InitializeComponent();
        }

        private static void IsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                SplashIcon comp = (SplashIcon)d;
                Storyboard sb = comp.FindResource("Storyboard1") as Storyboard;
                if (sb != null)
                    comp.BeginStoryboard(sb);
            }
        }


        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
    }
}
