using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Minecraft_Launcher_2
{
    /// <summary>
    /// ConsoleWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConsoleWindow : Window
    {
        private StreamWriter logWriter;

        public ConsoleWindow()
        {
            InitializeComponent();
            txtConsole.Document = new FlowDocument();

            string mcdir = Path.Combine(Properties.Settings.Default.MinecraftDir, "log", "client");
            if (!Directory.Exists(mcdir))
                Directory.CreateDirectory(mcdir);

            string logfile = Path.Combine(mcdir, "client-log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
            logWriter = File.AppendText(logfile);
        }

        public void Info(string text)
        {
            Println(text, false);
        }

        public void Error(string text)
        {
            Println(text, true);
        }

        public void ShowWindow()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
            });
        }

        public void Println(string text, bool isError)
        {
            if (text == null)
                return;

            logWriter.WriteLine(text);
            logWriter.Flush();

            Dispatcher.Invoke(() =>
            {
                Paragraph p = new Paragraph();
                string[] info = SeperateInfo(text);
                SolidColorBrush col;

                if (isError)
                {
                    col = new SolidColorBrush(Colors.OrangeRed);
                }
                else
                {
                    col = new SolidColorBrush(Colors.Black);
                }

                if (!string.IsNullOrEmpty(info[0]))
                {
                    Run r = new Run(info[0]);
                    r.FontWeight = FontWeights.Bold;

                    p.Inlines.Add(r);
                }

                if (!string.IsNullOrEmpty(info[1]))
                {
                    p.Foreground = col;
                    p.Inlines.Add(info[1]);
                }

                txtConsole.Document.Blocks.Add(p);
                if (chbAutoScroll.IsChecked == true)
                {
                    txtConsole.ScrollToEnd();
                }
            });
        }

        private string[] SeperateInfo(string str)
        {
            string[] ret = new string[2];

            if (str.StartsWith("[") && str.Contains("]:"))
            {
                int index = str.IndexOf("]:") + 2;
                ret[0] = str.Substring(0, index);
                ret[1] = str.Substring(index);
            }
            else
            {
                ret[1] = str;
            }

            return ret;
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            txtConsole.Document.Blocks.Clear();
        }

        private void SaveConsole_Click(object sender, RoutedEventArgs e)
        {
            TextRange r = new TextRange(txtConsole.Document.ContentStart, txtConsole.Document.ContentEnd);

            if (r.CanSave(System.Windows.DataFormats.Text))
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.FileName = "console_log.txt";

                if (dialog.ShowDialog() == true)
                {
                    FileStream file = new FileStream(dialog.FileName, FileMode.Create);
                    r.Save(file, System.Windows.DataFormats.Text);
                    MessageBox.Show("저장되었습니다.");
                }
            }
            else
            {
                MessageBox.Show("Text 포맷으로 저장할 수 없습니다.", "오류.");
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (chbAutoScroll.IsChecked == true)
            {
                txtConsole.ScrollToEnd();
            }
        }
    }
}
