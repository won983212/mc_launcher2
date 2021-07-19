using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    public class CommonUtils
    {
        public delegate void DialogCompleteEventHandler<T>(T vm, DialogClosingEventArgs eventArgs) where T : ObservableObject;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);



        public static int GetTotalMemorySizeGB()
        {
            GetPhysicallyInstalledSystemMemory(out long memKb);
            return (int)(memKb / 1024 / 1024);
        }

        public static bool IsLegalUsername(string name)
        {
            foreach (char c in name)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".IndexOf(c) == -1)
                    return false;
            }

            return true;
        }

        public static async Task<bool> IsActiveHttpServer(string url)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = APIServerInfoRetriever.Timeout;
                req.AllowAutoRedirect = false;
                req.Method = "HEAD";

                // GetResponseAsync는 Timeout이 적용되지 않으므로 이 방법을 사용해야함.
                WebResponse resp = await Task.Run(req.GetResponse);
                resp.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string SelectDirectory(string title, string initialPath = "C:/users")
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = title;
            dialog.InitialDirectory = initialPath;
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrWhiteSpace(dialog.FileName))
                return null;

            return dialog.FileName;
        }

        public static void CopyDirectory(string src, string dest, Action<ProgressArgs> onProcess = null)
        {
            int total = 0;
            int current = 0;
            Stack<string> folders = new Stack<string>();
            folders.Push(src);

            if (onProcess != null)
            {
                while (folders.Count > 0)
                {
                    string path = folders.Pop();
                    foreach (string dir in Directory.EnumerateDirectories(path))
                        folders.Push(dir);
                    total += Directory.GetFiles(path).Length;
                }
            }

            folders.Push(src);
            while (folders.Count > 0)
            {
                string path = folders.Pop();
                foreach (string dir in Directory.EnumerateDirectories(path))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dir.Substring(src.Length + 1)));
                    folders.Push(dir);
                }
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    File.Copy(file, Path.Combine(dest, file.Substring(src.Length + 1)));
                    onProcess?.Invoke(new ProgressArgs(++current / (double)total * 100, Path.GetFileName(file)));
                }
            }
        }

        public static void DeleteDirectory(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            if (di.Exists)
                di.Delete(true);
        }

        public static Task<object> ShowDialog<T>(T content, DialogCompleteEventHandler<T> closingHandler = null) where T : ObservableObject
        {
            return ShowDialog(content, "RootDialogHost", closingHandler);
        }

        public static Task<object> ShowDialog<T>(T content, string dialog, DialogCompleteEventHandler<T> closingHandler = null) where T : ObservableObject
        {
            return DialogHost.Show(content, dialog, (o, e) => closingHandler?.Invoke((T)((DialogHost)o).DialogContent, e));
        }

        public static void CloseDialog()
        {
            DialogHost.Close("RootDialogHost");
        }
    }
}
