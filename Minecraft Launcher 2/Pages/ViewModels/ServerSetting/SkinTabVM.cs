using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Minecraft_Launcher_2.Pages.ViewModels.ServerSetting
{
    internal class SkinTabVM : TabChild<ServerSettingPanelVM>
    {
        private readonly ObservableCollection<PlayerSkin> skins = new ObservableCollection<PlayerSkin>();


        public SkinTabVM(ServerSettingPanelVM parent)
            : base(parent)
        {
            parent.PanelOpened += Parent_PanelOpened;
        }


        public IEnumerable<PlayerSkin> Skins => skins;

        public ICommand AddSkinCommand => new RelayCommand(AddSkin);


        private void Parent_PanelOpened(object sender, EventArgs e)
        {
            LoadSkinData();
        }

        private void AddSkin()
        {

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
