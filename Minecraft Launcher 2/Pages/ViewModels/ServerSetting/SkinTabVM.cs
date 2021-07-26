using Minecraft_Launcher_2.Pages.ViewModels.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Minecraft_Launcher_2.Pages.ViewModels.ServerSetting
{
    internal class SkinTabVM : TabChild<ServerSettingPanelVM>
    {
        private const int HashLen = 30;
        private readonly ObservableCollection<PlayerSkin> skins = new ObservableCollection<PlayerSkin>();


        public SkinTabVM(ServerSettingPanelVM parent)
            : base(parent)
        {
            parent.PanelOpened += Parent_PanelOpened;
        }


        public IEnumerable<PlayerSkin> Skins => skins;

        public ICommand AddSkinCommand => new RelayCommand(async () =>
        {
            if(await AddSkin())
                MessageBox.Show("스킨이 추가되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        });


        private void Parent_PanelOpened(object sender, EventArgs e)
        {
            LoadSkinData();
        }

        private async Task<bool> AddSkin()
        {
            PromptMessageBoxVM vm = new PromptMessageBoxVM("추가할 사용자의 닉네임을 입력하세요");
            await CommonUtils.ShowDialog(vm);

            try
            {
                string username = vm.InputText;
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentException("사용자의 이름을 반드시 입력해야합니다.");

                string path = CommonUtils.SelectFile("스킨파일 선택", 
                    (filters) => filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("PNG Files", ".png")));
                if (path == null)
                    return false;

                if (Path.GetExtension(path) != ".png")
                    throw new ArgumentException("스킨의 확장자는 png이어야 합니다.");

                string textureDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures");
                if (!Directory.Exists(textureDir))
                    Directory.CreateDirectory(textureDir);

                PlayerSkin skin = new PlayerSkin(username) { SkinFilename = CommonUtils.GenerateHashUnique(HashLen, textureDir) };
                File.Copy(path, Path.Combine(textureDir, skin.SkinFilename));
                skin.LoadHeadImage();
                skin.SaveJson();

                skins.Add(skin);
                return true;
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        // TODO Must be call in async
        private void LoadSkinData()
        {
            skins.Clear();
            string skinDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin");

            if (!Directory.Exists(skinDir))
                Directory.CreateDirectory(skinDir);

            string player_name = "???";
            foreach (string file in Directory.EnumerateFiles(skinDir))
            {
                try
                {
                    if (Path.GetExtension(file) == ".json")
                    {
                        player_name = Path.GetFileNameWithoutExtension(file);
                        JObject obj = JObject.Parse(File.ReadAllText(file));
                        PlayerSkin skin = new PlayerSkin(player_name)
                        {
                            SkinFilename = obj.Value<string>("skin"),
                            CapeFilename = obj.Value<string>("cape"),
                            ElytraFilename = obj.Value<string>("elytra")
                        };

                        skin.LoadHeadImage();
                        skins.Add(skin);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed load " + player_name + ": " + e.ToString());
                }
            }
        }
    }

    internal class PlayerSkin
    {
        public PlayerSkin(string name)
        {
            PlayerName = name;
        }

        public BitmapSource SkinHeadImage { get; set; }
        public string SkinFilename { get; set; }
        public string CapeFilename { get; set; }
        public string ElytraFilename { get; set; }
        public string PlayerName { get; }


        public void LoadHeadImage()
        {
            string textureDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures");
            if (!Directory.Exists(textureDir))
                Directory.CreateDirectory(textureDir);

            if (SkinFilename == null)
                throw new InvalidDataException("There is no skin file name.");

            string skinFile = Path.Combine(textureDir, SkinFilename);
            if (!File.Exists(skinFile))
                throw new InvalidDataException("Not exists skin file.");

            BitmapImage skinImage = new BitmapImage(new Uri(skinFile));
            SkinHeadImage = new CroppedBitmap(skinImage, new System.Windows.Int32Rect(8, 8, 8, 8));
        }

        public void SaveJson()
        {
            JObject json = new JObject();
            json.Add("username", PlayerName);
            json.Add("skin", SkinFilename);
            json.Add("cape", CapeFilename);
            json.Add("elytra", ElytraFilename);

            string skinDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin");
            if (!Directory.Exists(skinDir))
                Directory.CreateDirectory(skinDir);

            // write file
            File.WriteAllText(Path.Combine(skinDir, PlayerName + ".json"), json.ToString());
        }
    }
}
