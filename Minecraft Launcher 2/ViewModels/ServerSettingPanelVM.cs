using Minecraft_Launcher_2.Controls;
using Minecraft_Launcher_2.Dialogs.ViewModels;
using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.Updater;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
            string infoFilePath = Path.Combine(path, URLs.InfoFilename);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(infoFilePath))
            {
                try
                {
                    LoadServerInfo(infoFilePath);
                }
                catch (Exception)
                {
                    return false;
                }
                return Version != null && WelcomeMessage != null;
            }
            return false;
        }

        private bool ResetAPIServerDirectory()
        {
            Settings setting = Settings.Default;
            string path = CommonUtils.SelectDirectory("API Server의 Root폴더 선택");

            if (path == null)
                return false;

            if (!IsVaildAPIServerDirectory(path))
            {
                MessageBox.Show("선택한 폴더는 올바른 API Server폴더가 아닙니다.");
                return false;
            }

            setting.APIServerDirectory = path;
            setting.Save();
            return true;
        }

        private void LoadServerInfo(string infoFilePath)
        {
            JObject json = JObject.Parse(File.ReadAllText(infoFilePath));
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
                if (!IsVaildAPIServerDirectory(Settings.Default.APIServerDirectory) && !ResetAPIServerDirectory())
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

        private void UpdateVersionToToday()
        {
            Version = Version.Split(new char[] { '@' }, 2)[0] + '@' + DateTime.Now.ToString("yyMMdd");
        }

        private void UpgradeGameVersionImpl(string serverHtmlDir, string minecraftDir, string selectedProfile)
        {
            // extract json from target profile
            string versionsDir = Path.Combine(minecraftDir, "versions");
            string jsonPath = Path.Combine(versionsDir, selectedProfile, selectedProfile + ".json");
            JObject cur = JObject.Parse(File.ReadAllText(jsonPath));

            LaunchConfigContext launchConfig = new LaunchConfigContext();
            Stack<JObject> stack = new Stack<JObject>();
            stack.Push(cur);

            while (cur.ContainsKey("inheritsFrom"))
            {
                string parentPath = Path.Combine(versionsDir, (string)cur["inheritsFrom"], cur["inheritsFrom"] + ".json");
                JObject json = JObject.Parse(File.ReadAllText(parentPath));
                stack.Push(json);
                cur = json;
            }

            // merge all related jsons
            while (stack.Count > 0)
                launchConfig.DeserializeMinecraftJsonData(stack.Pop());

            ProgressData.SetProgress("launch-config 작성중...", 20);

            // write launch-config.json
            File.WriteAllText(Path.Combine(serverHtmlDir, URLs.LauncherConfigFilename), launchConfig.Serialize().ToString());

            string resourceDir = Path.Combine(serverHtmlDir, URLs.ResourceFolderName);
            if (!Directory.Exists(resourceDir))
                Directory.CreateDirectory(resourceDir);

            ProgressData.SetProgress("복사할 library 폴더 검색중...", 30);

            // Copy libraries
            string libraryFolder = Path.Combine(resourceDir, "libraries");
            CommonUtils.DeleteDirectory(libraryFolder);
            CommonUtils.CopyDirectory(Path.Combine(minecraftDir, "libraries"), libraryFolder,
                (arg) => ProgressData.SetProgress("Library 복사: " + arg.Status, 30 + arg.Progress * 0.7));

            // copy minecraft.jar
            ProgressData.SetProgress("minecraft.jar 복사중...", 100);
            string minecraftFile = Path.Combine(resourceDir, "minecraft.jar");
            if (File.Exists(minecraftFile))
                File.Delete(minecraftFile);
            File.Copy(Path.Combine(versionsDir, selectedProfile, selectedProfile + ".jar"), minecraftFile);
        }

        private async Task<bool> UpgradeGameVersion()
        {
            try
            {
                string serverHtmlPath = Settings.Default.APIServerDirectory;
                string resourceDir = Path.Combine(serverHtmlPath, URLs.ResourceFolderName);

                string minecraftDir = CommonUtils.SelectDirectory("추출할 마인크래프트 폴더 선택",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft"));

                if (minecraftDir == null)
                    return false;

                string versionsDir = Path.Combine(minecraftDir, "versions");
                if (!Directory.Exists(versionsDir))
                    throw new ArgumentException("마인크래프트 폴더에 versions폴더가 없습니다.");

                string[] profileDirs = Directory.GetDirectories(versionsDir).Select((path) => Path.GetFileName(path)).ToArray();
                if (profileDirs.Length == 0)
                    throw new ArgumentException("선택가능한 프로필이 없습니다.");

                int selected = 0;
                await CommonUtils.ShowDialog(new ComboMessageBoxVM("추출할 프로필을 선택하세요.", profileDirs), (vm, e) => selected = vm.SelectedIndex);

                if (selected < 0 || selected >= profileDirs.Length)
                    throw new ArgumentException("프로필을 선택해주세요.");

                ProgressData.IsShow = true;
                ProgressData.SetProgress("프로필 병합중...", 0);

                await Task.Run(() => UpgradeGameVersionImpl(serverHtmlPath, minecraftDir, profileDirs[selected]));

                ProgressData.IsShow = false;

                // update file indexes
                await GenerateIndexJson();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private async Task<bool> GenerateIndexJson()
        {
            SHA1Managed sha1 = new SHA1Managed();
            try
            {
                string serverHtmlPath = Settings.Default.APIServerDirectory;
                string indexFile = Path.Combine(serverHtmlPath, URLs.IndexFilename);
                string resourceDir = Path.Combine(serverHtmlPath, URLs.ResourceFolderName);

                ProgressData.IsShow = true;
                ProgressData.SetProgress("resource파일 탐색중..", 0);

                if (File.Exists(indexFile))
                    File.Delete(indexFile);

                if (!Directory.Exists(resourceDir))
                {
                    if (File.Exists(resourceDir))
                        throw new ArgumentException("resources폴더를 생성할 수 없습니다. 같은 이름의 파일이 이미 존재합니다.");
                    Directory.CreateDirectory(resourceDir);
                }

                int total = 0;
                int pass = 0;
                JObject json = new JObject();
                Stack<string> folders = new Stack<string>();
                folders.Push(resourceDir);

                await Task.Run(() =>
                {
                    // Count All Files
                    while (folders.Count > 0)
                    {
                        string folder = folders.Pop();
                        total += Directory.GetFiles(folder).Length;
                        foreach (string dir in Directory.EnumerateDirectories(folder))
                            folders.Push(dir);
                    }

                    // Generate Hash
                    folders.Push(resourceDir);
                    while (folders.Count > 0)
                    {
                        string folder = folders.Pop();
                        foreach (string file in Directory.EnumerateFiles(folder))
                        {
                            string hash = SHA1(sha1, file);
                            string name = file.Substring(resourceDir.Length + 1).Replace("\\", "/");
                            json.Add(name, new JObject(new JProperty("hash", hash), new JProperty("size", new FileInfo(file).Length)));
                            Logger.Info("Generated Hash: " + name);
                            ProgressData.SetProgress("Hash 생성: " + name, ++pass / (double)total * 100);
                        }
                        foreach (string dir in Directory.EnumerateDirectories(folder))
                            folders.Push(dir);
                    }

                    ProgressData.SetProgress("생성한 Hash 파일로 작성중...", 100);
                    json = new JObject(new JProperty("objects", json));
                    File.WriteAllText(indexFile, json.ToString());
                });

                ProgressData.IsShow = false;
                if (Settings.Default.UseAutoRefreshVersion)
                    UpdateVersionToToday();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                sha1.Dispose();
            }
            return false;
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

        public ICommand UpdateVersionToDateCommand => new RelayCommand(UpdateVersionToToday);

        public ICommand UpdateFileHashCommand => new RelayCommand(async () =>
        {
            if (await GenerateIndexJson())
                MessageBox.Show(URLs.IndexFilename + " 파일이 생성되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand UpgradeGameVersionCommand => new RelayCommand(async () =>
        {
            if (await UpgradeGameVersion())
                MessageBox.Show("업그레이드가 완료되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand ChangeAPIServerDirectoryCommand => new RelayCommand(() => ResetAPIServerDirectory());

        public ICommand CloseCommand => new RelayCommand(Close);
    }
}
