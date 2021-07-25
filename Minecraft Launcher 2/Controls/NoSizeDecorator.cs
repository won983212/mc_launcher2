using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Launcher_2.Controls
{
    public class NoSizeDecorator : Decorator
    {
        protected override Size MeasureOverride(Size constraint)
        {
            if (Child != null)
            {
                Child.Measure(new Size(0, 0));
            }

            return new Size(0, 0);
        }
    }
}