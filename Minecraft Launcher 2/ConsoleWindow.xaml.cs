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
            UpdateFilter(tbxFilter.Text, chbOnlyDisplayError.IsChecked == true);
        }

        private void ScrollToEnd()
        {
            if (listLog.Items.Count > 0)
                listLog.ScrollIntoView(listLog.Items[listLog.Items.Count - 1]);
        }

        private void UpdateFilter(string filter, bool onlyDisplayError)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(Logger.Logs);
            view.Filter = (o) =>
            {
                LogMessage log = (LogMessage)o;
                if (onlyDisplayError && log.Type != LogType.Error)
                    return false;

                if (string.IsNullOrEmpty(filter))
                    return true;

                return log.Message.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) != -1;
            };
            listLog.ItemsSource = view;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
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

        private void Checkbox_AutoScroll_Click(object sender, RoutedEventArgs e)
        {
            if (chbAutoScroll.IsChecked == true)
                ScrollToEnd();
        }

        private void Checkbox_OnlyDisplayError_Click(object sender, RoutedEventArgs e)
        {
            UpdateFilter(tbxFilter.Text, chbOnlyDisplayError.IsChecked == true);
        }

        private void TextBox_FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilter(tbxFilter.Text, chbOnlyDisplayError.IsChecked == true);
        }
    }
}
