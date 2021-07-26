using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    public class CommonUtils
    {
        public delegate void DialogCompleteEventHandler<T>(T vm, DialogClosingEventArgs eventArgs)
            where T : ObservableObject;

        public delegate void FilterSelector(CommonFileDialogFilterCollection filters);


        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
        private static readonly string hashChars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
        private static readonly Random random = new Random();


        public static string GetOrCreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GenerateHashUnique(int len, string directoryPath)
        {
            string filename;
            do
            {
                filename = GenerateHash(len);
            }
            while (File.Exists(Path.Combine(directoryPath, filename)));
            return filename;
        }

        public static string GenerateHash(int len)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
                sb.Append(hashChars[random.Next(hashChars.Length)]);
            return sb.ToString();
        }

        public static int GetTotalMemorySizeGB()
        {
            GetPhysicallyInstalledSystemMemory(out var memKb);
            return (int)(memKb / 1024 / 1024);
        }

        public static bool IsLegalUsername(string name)
        {
            foreach (var c in name)
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
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = APIServerInfoRetriever.Timeout;
                req.AllowAutoRedirect = false;
                req.Method = "HEAD";

                // GetResponseAsync는 Timeout이 적용되지 않으므로 이 방법을 사용해야함.
                var resp = await Task.Run(req.GetResponse);
                resp.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string SelectFile(string title, FilterSelector filterSelector = null, string initialPath = "C:/users")
        {
            return SelectFileWithDialog(title, filterSelector, initialPath, false);
        }

        public static string SelectDirectory(string title, FilterSelector filterSelector = null, string initialPath = "C:/users")
        {
            return SelectFileWithDialog(title, filterSelector, initialPath, true);
        }

        private static string SelectFileWithDialog(string title, FilterSelector filterSelector, string initialPath, bool isDir)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Title = title;
            dialog.InitialDirectory = initialPath;
            dialog.IsFolderPicker = isDir;

            if (filterSelector != null)
                filterSelector(dialog.Filters);

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrWhiteSpace(dialog.FileName))
                return null;

            return dialog.FileName;
        }

        public static void CopyDirectory(string src, string dest, Action<ProgressArgs> onProcess = null)
        {
            var total = 0;
            var current = 0;
            var folders = new Stack<string>();
            folders.Push(src);

            if (onProcess != null)
            {
                while (folders.Count > 0)
                {
                    var path = folders.Pop();
                    foreach (var dir in Directory.EnumerateDirectories(path))
                        folders.Push(dir);
                    total += Directory.GetFiles(path).Length;
                }
            }

            folders.Push(src);
            while (folders.Count > 0)
            {
                var path = folders.Pop();
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dir.Substring(src.Length + 1)));
                    folders.Push(dir);
                }

                foreach (var file in Directory.EnumerateFiles(path))
                {
                    File.Copy(file, Path.Combine(dest, file.Substring(src.Length + 1)));
                    onProcess?.Invoke(new ProgressArgs(++current / (double)total * 100, Path.GetFileName(file)));
                }
            }
        }

        public static void DeleteDirectory(string path)
        {
            var di = new DirectoryInfo(path);
            if (di.Exists)
                di.Delete(true);
        }

        public static Task<object> ShowDialog<T>(T content, DialogCompleteEventHandler<T> closingHandler = null)
            where T : ObservableObject
        {
            return ShowDialog(content, "RootDialogHost", closingHandler);
        }

        public static Task<object> ShowDialog<T>(T content, string dialog,
            DialogCompleteEventHandler<T> closingHandler = null) where T : ObservableObject
        {
            return DialogHost.Show(content, dialog,
                (o, e) => closingHandler?.Invoke((T)((DialogHost)o).DialogContent, e));
        }

        public static void CloseDialog()
        {
            DialogHost.Close("RootDialogHost");
        }
    }
}