using System.Collections.Generic;

namespace Minecraft_Launcher_2.Pages.ViewModels
{
    public class TabContainer : ObservableObject
    {
        private TabChild _currentPage;
        private int _selectedTabItemIndex = 0;

        protected List<TabChild> Tabs { get; } = new List<TabChild>();

        public int SelectedTabItemIndex
        {
            get => _selectedTabItemIndex;
            set
            {
                _selectedTabItemIndex = value;
                CurrentPage = Tabs[_selectedTabItemIndex];
                OnPropertyChanged();
            }
        }

        public TabChild CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }


        protected void AddTab(TabChild tab)
        {
            Tabs.Add(tab);
        }
    }
}
