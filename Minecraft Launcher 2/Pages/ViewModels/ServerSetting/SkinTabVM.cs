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

        private int _selectedSkinIndex = -1;


        public SkinTabVM(ServerSettingPanelVM parent)
            : base(parent)
        {
            parent.PanelOpened += Parent_PanelOpened;
        }


        public IEnumerable<PlayerSkin> Skins => skins;

        public int SelectedSkinIndex
        {
            get => _selectedSkinIndex;
            set => SetProperty(ref _selectedSkinIndex, value);
        }

        public ICommand AddSkinCommand => new RelayCommand(async () =>
        {
            if (await AddSkin())
                MessageBox.Show("스킨이 추가되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand RemoveSkinCommand => new RelayCommand(RemoveSkinData);

        public ICommand ChangeSkinCommand => new RelayCommand<SkinType>(ChangeSkin);

        public ICommand RenameUserCommand => new RelayCommand(RenameUser);


        private void Parent_PanelOpened(object sender, EventArgs e)
        {
            LoadSkinData();
        }

        private async void RenameUser()
        {
            if (SelectedSkinIndex < 0 || SelectedSkinIndex >= skins.Count)
                return;

            PlayerSkin skin = skins[SelectedSkinIndex];
            PromptMessageBoxVM vm = new PromptMessageBoxVM("변경할 닉네임을 입력하세요.", skin.PlayerName);
            await CommonUtils.ShowDialog(vm);

            try
            {
                string username = vm.InputText;
                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentException("사용자의 이름을 반드시 입력해야합니다.");

                string prevName = skin.PlayerName;
                skin.Rename(username);
                MessageBox.Show(prevName + "이었던 이름을 " + username + "으로 변경되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeSkin(SkinType type)
        {
            if (SelectedSkinIndex < 0 || SelectedSkinIndex >= skins.Count)
                return;

            PlayerSkin skin = skins[SelectedSkinIndex];
            MessageBoxResult result = MessageBox.Show("선택한 " + skin.PlayerName + "의 스킨을 변경합니까? 아니오를 누르면 삭제(null로 변경)하고, 취소를 누르면 작업을 취소합니다.", "변경? 삭제?",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            try
            {
                if (result == MessageBoxResult.No)
                {
                    skin.SetFilename(type, null);
                    skin.SaveJson();
                    MessageBox.Show("스킨이 삭제되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string path = CommonUtils.SelectFile("스킨파일 선택",
                        (filters) => filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("PNG Files", ".png")));
                if (path == null)
                    return;

                if (Path.GetExtension(path) != ".png")
                    throw new ArgumentException("스킨의 확장자는 png이어야 합니다.");

                string textureDir = CommonUtils.GetOrCreateDirectory(Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures"));
                string skinFilename = skin.GetFilename(type);
                if (skinFilename != null)
                    File.Delete(Path.Combine(textureDir, skinFilename));
                else
                    skinFilename = CommonUtils.GenerateHashUnique(HashLen, textureDir);

                File.Copy(path, Path.Combine(textureDir, skinFilename));
                skin.SetFilename(type, skinFilename);
                skin.SaveJson();
                skin.LoadHeadImage();

                MessageBox.Show("스킨이 변경되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void RemoveSkinData()
        {
            if (SelectedSkinIndex < 0 || SelectedSkinIndex >= skins.Count)
                return;

            PlayerSkin skin = skins[SelectedSkinIndex];
            MessageBoxResult result = MessageBox.Show("정말로 " + skin.PlayerName + "의 스킨을 삭제하시겠습니까?", "주의",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                skin.RemoveSkinData();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            skins.Remove(skin);
            MessageBox.Show("스킨이 삭제되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
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

                string textureDir = CommonUtils.GetOrCreateDirectory(Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures"));
                string skinFile = CommonUtils.GenerateHashUnique(HashLen, textureDir);
                PlayerSkin skin = new PlayerSkin(username);
                skin.SetFilename(SkinType.Skin, skinFile);

                File.Copy(path, Path.Combine(textureDir, skinFile));
                skin.LoadHeadImage();
                skin.SaveJson();

                skins.Add(skin);
                return true;
            }
            catch (ArgumentException e)
            {
                Logger.Error(e);
                MessageBox.Show(e.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private void LoadSkinData()
        {
            skins.Clear();

            string skinDir = CommonUtils.GetOrCreateDirectory(Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin"));
            string player_name = "???";
            foreach (string file in Directory.EnumerateFiles(skinDir))
            {
                try
                {
                    if (Path.GetExtension(file) == ".json")
                    {
                        player_name = Path.GetFileNameWithoutExtension(file);
                        JObject obj = JObject.Parse(File.ReadAllText(file));
                        PlayerSkin skin = new PlayerSkin(player_name);

                        foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
                            skin.SetFilename(type, obj.Value<string>(Enum.GetName(typeof(SkinType), type).ToLower()));

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

    internal enum SkinType
    {
        Skin, Cape, Elytra
    }

    internal class PlayerSkin : ObservableObject
    {
        private readonly Dictionary<SkinType, string> filenames = new Dictionary<SkinType, string>();
        private BitmapSource skinHeadImage = null;
        private string playerName = "";


        public PlayerSkin(string name)
        {
            PlayerName = name;
        }

        public BitmapSource SkinHeadImage
        {
            get => skinHeadImage;
            private set => SetProperty(ref skinHeadImage, value);
        }

        public string PlayerName
        {
            get => playerName;
            private set => SetProperty(ref playerName, value);
        }


        public string GetFilename(SkinType type)
        {
            if (filenames.TryGetValue(type, out string value))
                return value;
            return null;
        }

        /**
         * value로 null을 보내면 해당 type의 스킨을 삭제합니다. (dictionary entry, file 둘 다 삭제)
         */
        public void SetFilename(SkinType type, string value)
        {
            if (value == null)
            {
                if (type == SkinType.Skin)
                {
                    RemoveSkinData();
                }
                else
                {
                    string prevSkin = GetFilename(type);
                    if (prevSkin != null)
                    {
                        string textureDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures");
                        if (!Directory.Exists(textureDir))
                            return;
                        File.Delete(Path.Combine(textureDir, prevSkin));
                    }
                }
            }
            filenames[type] = value;
        }

        public void LoadHeadImage()
        {
            string textureDir = CommonUtils.GetOrCreateDirectory(Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures"));

            string skinFileName = GetFilename(SkinType.Skin);
            if (skinFileName == null)
                throw new InvalidDataException("There is no skin file name.");

            string skinFile = Path.Combine(textureDir, skinFileName);
            if (!File.Exists(skinFile))
                throw new InvalidDataException("Not exists skin file.");

            BitmapImage skinImage = new BitmapImage();
            skinImage.BeginInit();
            skinImage.CacheOption = BitmapCacheOption.OnLoad;
            skinImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            skinImage.UriSource = new Uri(skinFile);
            skinImage.EndInit();

            SkinHeadImage = new CroppedBitmap(skinImage, new Int32Rect(8, 8, 8, 8));
        }

        public void SaveJson()
        {
            JObject json = new JObject();
            json.Add("username", PlayerName);

            foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
                json.Add(Enum.GetName(typeof(SkinType), type).ToLower(), GetFilename(type));

            string skinDir = CommonUtils.GetOrCreateDirectory(Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin"));

            // write file
            File.WriteAllText(Path.Combine(skinDir, PlayerName + ".json"), json.ToString());
        }

        public void RemoveSkinData()
        {
            string skinDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin");
            if (!Directory.Exists(skinDir))
                return;

            File.Delete(Path.Combine(skinDir, PlayerName + ".json"));

            string textureDir = Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin/textures");
            if (!Directory.Exists(textureDir))
                return;

            foreach (SkinType type in Enum.GetValues(typeof(SkinType)))
            {
                string file = GetFilename(type);
                if (file != null)
                    File.Delete(Path.Combine(textureDir, file));
            }
        }

        public void Rename(string name)
        {
            string skinDir = CommonUtils.GetOrCreateDirectory(Path.Combine(Properties.Settings.Default.APIServerDirectory, "skin"));
            if (File.Exists(Path.Combine(skinDir, name + ".json")))
                throw new ArgumentException(name + "은 이미 존재하는 이름입니다.");

            File.Delete(Path.Combine(skinDir, PlayerName + ".json"));
            PlayerName = name;
            SaveJson();
        }
    }
}
