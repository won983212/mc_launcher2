namespace Minecraft_Launcher_2.Pages.ViewModels
{
    public class TabChild : ObservableObject
    {
    }

    public class TabChild<T> : TabChild where T : TabContainer
    {
        public T Parent { get; }

        public TabChild(T parent)
        {
            Parent = parent;
        }
    }
}
