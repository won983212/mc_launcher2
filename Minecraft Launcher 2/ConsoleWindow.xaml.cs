using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Minecraft_Launcher_2
{
    public partial class ConsoleWindow : Window
    {
        public ConsoleWindow()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)listLog.Items).CollectionChanged += ConsoleWindow_CollectionChanged;
            UpdateFilter("");

            var timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
            var r = new Random();
            timer.Tick += (o, e) =>
            {
                switch (r.Next(0, 3))
                {
                    case 0:
                        Logger.Log("How are you? " + DateTime.Now);
                        break;
                    case 1:
                        Logger.Debug("Hello, world! " + DateTime.Now);
                        break;
                    case 2:
                        Logger.Error("ERROR DATE " + DateTime.Now);
                        break;
                    default:
                        break;
                }
            };
            timer.Start();
        }

        private void ConsoleWindow_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (chbAutoScroll.IsChecked == true)
                ScrollToEnd();
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            Logger.ClearLogs();
        }

        private void SaveConsole_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { FileName = "console_log.txt" };

            if (dialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(dialog.FileName, FileMode.Create)))
                {
                    foreach (LogMessage message in Logger.Logs)
                    {
                        writer.WriteLine("[" + message.Type.ToString() + "] " + message.Message);
                    }
                }
                MessageBox.Show("저장되었습니다.");
            }
        }

        private void ScrollToEnd()
        {
            if (listLog.Items.Count > 0)
                listLog.ScrollIntoView(listLog.Items[listLog.Items.Count - 1]);
        }

        private void UpdateFilter(string filter)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(Logger.Logs);
            view.Filter = (o) => string.IsNullOrEmpty(filter) || ((LogMessage)o).Message.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) != -1;
            listLog.ItemsSource = view;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (chbAutoScroll.IsChecked == true)
                ScrollToEnd();
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateFilter(((TextBox)sender).Text);
        }
    }
}
