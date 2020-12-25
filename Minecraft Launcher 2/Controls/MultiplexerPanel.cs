using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Controls
{
    class MultiplexerPanel : Panel
    {
        public static readonly DependencyProperty ActiveChildIndexProperty = DependencyProperty.Register("ActiveChildIndex",
            typeof(int), typeof(MultiplexerPanel), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsArrange, OnActiveChildIndexChanged));

        public int ActiveChildIndex
        {
            get => (int)GetValue(ActiveChildIndexProperty);
            set { SetValue(ActiveChildIndexProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach(UIElement child in InternalChildren)
                child.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect rect = new Rect(0, 0, finalSize.Width, finalSize.Height);
            foreach (UIElement child in InternalChildren)
                child.Arrange(rect);
            return finalSize;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            foreach (UIElement child in InternalChildren)
                child.Visibility = Visibility.Collapsed;
            UpdateVisibility(ActiveChildIndex, ActiveChildIndex);
        }

        private void UpdateVisibility(int oldValue, int newValue)
        {
            if (InternalChildren.Count > oldValue)
            {
                InternalChildren[oldValue].Visibility = Visibility.Collapsed;
            }
            if (InternalChildren.Count > newValue)
            {
                InternalChildren[newValue].Visibility = Visibility.Visible;
            }
        }

        private static void OnActiveChildIndexChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MultiplexerPanel panel = sender as MultiplexerPanel;
            panel.UpdateVisibility((int)e.OldValue, (int)e.NewValue);
        }
    }
}
