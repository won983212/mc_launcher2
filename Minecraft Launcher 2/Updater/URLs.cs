namespace Minecraft_Launcher_2.Updater
{
    public static class URLs
    {
        private static readonly Properties.Settings settings = Properties.Settings.Default;

        public static readonly string InfoFilename = "info.json";
        public static readonly string IndexFilename = "indexes.json";
        public static readonly string ResourceFolderName = "resources";
        public static readonly string LauncherConfigFilename = "launch-config.json";

        public static string InfoFilePath => settings.APIServerLocation + "/" + InfoFilename;
        public static string IndexFilePath => settings.APIServerLocation + "/" + IndexFilename;
        public static string PatchFolderPath => settings.APIServerLocation + "/" + ResourceFolderName;
        public static string LauncherConfigPath => settings.APIServerLocation + "/" + LauncherConfigFilename;

        public static readonly string LocalInfoFile = "http://127.0.0.1/" + InfoFilename;
        public static readonly string AssetsResourceURL = "http://resources.download.minecraft.net/";
    }
}
