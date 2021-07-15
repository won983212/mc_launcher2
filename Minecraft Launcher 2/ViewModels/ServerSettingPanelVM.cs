using Minecraft_Launcher_2.Controls;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.Updater;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Launcher_2.ViewModels
{
    public class ServerSettingPanelVM : ObservableObject
    {
        public event EventHandler PanelClosed;

        private string _version = null;
        private string _welcomeMessage = null;
        private bool _isShow = false;

        private bool IsVaildAPIServerDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, URLs.InfoFilename)))
            {
                LoadServerInfo();
                return Version != null && WelcomeMessage != null;
            }
            return false;
        }

        private bool ResetAPIServerDirectory()
        {
            Settings setting = Settings.Default;
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    return true;

                if (!IsVaildAPIServerDirectory(fbd.SelectedPath))
                {
                    MessageBox.Show("선택한 폴더는 올바른 API Server폴더가 아닙니다.");
                    return true;
                }

                setting.APIServerDirectory = fbd.SelectedPath;
                setting.Save();
            }
            return false;
        }

        private void LoadServerInfo()
        {
            string path = Settings.Default.APIServerDirectory;
            JObject json = JObject.Parse(File.ReadAllText(Path.Combine(path, URLs.InfoFilename)));
            Version = json.Value<string>("patchVersion");
            WelcomeMessage = json.Value<string>("notice");
        }

        private void SaveServerInfo()
        {
            string path = Settings.Default.APIServerDirectory;
            JObject json = new JObject(new JProperty("patchVersion", Version), new JProperty("notice", WelcomeMessage));
            File.WriteAllText(Path.Combine(path, URLs.InfoFilename), json.ToString());
        }

        public void Open()
        {
            if (!IsShow)
            {
                if (!IsVaildAPIServerDirectory(Settings.Default.APIServerDirectory) && ResetAPIServerDirectory())
                    return;
                IsShow = true;
            }
        }

        public void Close()
        {
            if (IsShow)
            {
                Settings.Default.Save();
                SaveServerInfo();
                IsShow = false;
                PanelClosed?.Invoke(this, null);
            }
        }

        private void GenerateIndexJson()
        {
            string serverHtmlPath = Settings.Default.APIServerDirectory;
            string indexFile = Path.Combine(serverHtmlPath, URLs.IndexFilename);
            string resourceDir = Path.Combine(serverHtmlPath, URLs.ResourceFolderName);

            if (File.Exists(indexFile))
                File.Delete(indexFile);

            if (!Directory.Exists(resourceDir))
            {
                if (File.Exists(resourceDir))
                    throw new ArgumentException("resources폴더를 생성할 수 없습니다. 같은 이름의 파일이 이미 존재합니다.");
                Directory.CreateDirectory(resourceDir);
            }

            JObject json = new JObject();
            SHA1Managed sha1 = new SHA1Managed();
            Stack<string> folders = new Stack<string>();
            folders.Push(resourceDir);

            while (folders.Count > 0)
            {
                string folder = folders.Pop();
                foreach (string file in Directory.GetFiles(folder))
                {
                    string hash = SHA1(sha1, file);
                    string name = file.Substring(resourceDir.Length + 1).Replace("\\", "/");
                    json.Add(name, new JObject(new JProperty("hash", hash), new JProperty("size", new FileInfo(file).Length)));
                    Logger.Info("Generated Hash: " + name);
                }
                foreach (string dir in Directory.GetDirectories(folder))
                {
                    folders.Push(dir);
                }
            }

            json = new JObject(new JProperty("objects", json));
            File.WriteAllText(indexFile, json.ToString());
            sha1.Dispose();
        }

        private static string SHA1(SHA1Managed sha1, string path)
        {
            StringBuilder sb = new StringBuilder(40);
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                byte[] hash = sha1.ComputeHash(fs);
                foreach (byte b in hash)
                    sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }


        public ProgressStatus ProgressData { get; } = new ProgressStatus();

        public bool IsShow
        {
            get => _isShow;
            private set => SetProperty(ref _isShow, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ICommand UpdateVersionToDateCommand => new RelayCommand(() => Version = Version.Split(new char[] { '@' }, 2)[0] + '@' + DateTime.Now.ToString("yyMMdd"));

        public ICommand UpdateFileHashCommand => new RelayCommand(() =>
        {
            ProgressData.IsShow = true;
            ProgressData.SetProgress("", 0);
        });

        public ICommand UpgradeGameVersionCommand => new RelayCommand(() => { });

        public ICommand ChangeAPIServerDirectoryCommand => new RelayCommand(() => ResetAPIServerDirectory());

        public ICommand CloseCommand => new RelayCommand(Close);
    }
}
