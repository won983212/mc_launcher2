using Minecraft_Launcher_2.Launcher;
using Minecraft_Launcher_2.Pages.ViewModels.Dialogs;
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

namespace Minecraft_Launcher_2.Pages.ViewModels.ServerSetting
{
    internal class UpdateTabVM : TabChild<ServerSettingPanelVM>
    {
        public UpdateTabVM(ServerSettingPanelVM parent)
            : base(parent)
        { }


        public ICommand UpdateFileHashCommand => new RelayCommand(async () =>
        {
            if (await GenerateIndexJson())
                MessageBox.Show(URLs.IndexFilename + " 파일이 생성되었습니다.", "완료", MessageBoxButton.OK,
                    MessageBoxImage.Information);
        });

        public ICommand UpgradeGameVersionCommand => new RelayCommand(async () =>
        {
            if (await UpgradeGameVersion())
                MessageBox.Show("업그레이드가 완료되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        private void UpgradeGameVersionImpl(string serverHtmlDir, string minecraftDir, string selectedProfile)
        {
            // extract json from target profile
            var versionsDir = Path.Combine(minecraftDir, "versions");
            var jsonPath = Path.Combine(versionsDir, selectedProfile, selectedProfile + ".json");
            var cur = JObject.Parse(File.ReadAllText(jsonPath));

            var launchConfig = new LaunchConfigContext();
            var stack = new Stack<JObject>();
            stack.Push(cur);

            while (cur.ContainsKey("inheritsFrom"))
            {
                var parentPath = Path.Combine(versionsDir, (string)cur["inheritsFrom"], cur["inheritsFrom"] + ".json");
                var json = JObject.Parse(File.ReadAllText(parentPath));
                stack.Push(json);
                cur = json;
            }

            // merge all related jsons
            while (stack.Count > 0)
                launchConfig.DeserializeMinecraftJsonData(stack.Pop());

            Parent.SetProgress("launch-config 작성중...", 20);

            // write launch-config.json
            File.WriteAllText(Path.Combine(serverHtmlDir, URLs.LauncherConfigFilename),
                launchConfig.Serialize().ToString());

            var resourceDir = Path.Combine(serverHtmlDir, URLs.ResourceFolderName);
            if (!Directory.Exists(resourceDir))
                Directory.CreateDirectory(resourceDir);

            Parent.SetProgress("복사할 library 폴더 검색중...", 30);

            // Copy libraries
            var libraryFolder = Path.Combine(resourceDir, "libraries");
            CommonUtils.DeleteDirectory(libraryFolder);
            CommonUtils.CopyDirectory(Path.Combine(minecraftDir, "libraries"), libraryFolder,
                arg => Parent.SetProgress("Library 복사: " + arg.Status, 30 + arg.Progress * 0.7));

            // copy minecraft.jar
            Parent.SetProgress("minecraft.jar 복사중...", 100);
            var minecraftFile = Path.Combine(resourceDir, "minecraft.jar");
            if (File.Exists(minecraftFile))
                File.Delete(minecraftFile);
            File.Copy(Path.Combine(versionsDir, selectedProfile, selectedProfile + ".jar"), minecraftFile);
        }

        private async Task<bool> UpgradeGameVersion()
        {
            try
            {
                var serverHtmlPath = Settings.Default.APIServerDirectory;

                var minecraftDir = CommonUtils.SelectDirectory("추출할 마인크래프트 폴더 선택",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft"));

                if (minecraftDir == null)
                    return false;

                var versionsDir = Path.Combine(minecraftDir, "versions");
                if (!Directory.Exists(versionsDir))
                    throw new ArgumentException("마인크래프트 폴더에 versions폴더가 없습니다.");

                var profileDirs = Directory.GetDirectories(versionsDir).Select(path => Path.GetFileName(path))
                    .ToArray();
                if (profileDirs.Length == 0)
                    throw new ArgumentException("선택가능한 프로필이 없습니다.");

                var selected = 0;
                await CommonUtils.ShowDialog(new ComboMessageBoxVM("추출할 프로필을 선택하세요.", profileDirs),
                    (vm, e) => selected = vm.SelectedIndex);

                if (selected < 0 || selected >= profileDirs.Length)
                    throw new ArgumentException("프로필을 선택해주세요.");

                Parent.SetProgress("프로필 병합중...", 0);
                await Task.Run(() => UpgradeGameVersionImpl(serverHtmlPath, minecraftDir, profileDirs[selected]));
                Parent.SetProgress("", -1);

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
            var sha1 = new SHA1Managed();
            try
            {
                var serverHtmlPath = Settings.Default.APIServerDirectory;
                var indexFile = Path.Combine(serverHtmlPath, URLs.IndexFilename);
                var resourceDir = Path.Combine(serverHtmlPath, URLs.ResourceFolderName);

                Parent.SetProgress("resource파일 탐색중..", 0);

                if (File.Exists(indexFile))
                    File.Delete(indexFile);

                if (!Directory.Exists(resourceDir))
                {
                    if (File.Exists(resourceDir))
                        throw new ArgumentException("resources폴더를 생성할 수 없습니다. 같은 이름의 파일이 이미 존재합니다.");
                    Directory.CreateDirectory(resourceDir);
                }

                var total = 0;
                var pass = 0;
                var json = new JObject();
                var folders = new Stack<string>();
                folders.Push(resourceDir);

                await Task.Run(() =>
                {
                    // Count All Files
                    while (folders.Count > 0)
                    {
                        var folder = folders.Pop();
                        total += Directory.GetFiles(folder).Length;
                        foreach (var dir in Directory.EnumerateDirectories(folder))
                            folders.Push(dir);
                    }

                    // Generate Hash
                    folders.Push(resourceDir);
                    while (folders.Count > 0)
                    {
                        var folder = folders.Pop();
                        foreach (var file in Directory.EnumerateFiles(folder))
                        {
                            var hash = SHA1(sha1, file);
                            var name = file.Substring(resourceDir.Length + 1).Replace("\\", "/");
                            json.Add(name,
                                new JObject(new JProperty("hash", hash),
                                    new JProperty("size", new FileInfo(file).Length)));
                            Logger.Info("Generated Hash: " + name);
                            Parent.SetProgress("Hash 생성: " + name, ++pass / (double)total * 100);
                        }

                        foreach (var dir in Directory.EnumerateDirectories(folder))
                            folders.Push(dir);
                    }

                    Parent.SetProgress("생성한 Hash 파일로 작성중...", 100);
                    json = new JObject(new JProperty("objects", json));
                    File.WriteAllText(indexFile, json.ToString());
                });

                Parent.SetProgress("", -1);
                if (Settings.Default.UseAutoRefreshVersion)
                    Parent.UpdateVersionToToday();
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
            var sb = new StringBuilder(40);
            using (var fs = new FileStream(path, FileMode.Open))
            {
                var hash = sha1.ComputeHash(fs);
                foreach (var b in hash)
                    sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }
    }
}
