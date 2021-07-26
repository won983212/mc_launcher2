using Minecraft_Launcher_2.Properties;

namespace Minecraft_Launcher_2.Updater
{
    public static class URLs
    {
        private static readonly Settings settings = Settings.Default;

        public static readonly string InfoFilename = "info.json";
        public static readonly string IndexFilename = "indexes.json";
        public static readonly string LauncherConfigFilename = "launch-config.json";
        public static readonly string ResourceFolderName = "resources";
        public static readonly string SkinFolderName = "skin";

        public static readonly string LocalInfoFile = "http://127.0.0.1:" + settings.APIServerPort + "/" + InfoFilename;
        public static readonly string AssetsResourceUrl = "http://resources.download.minecraft.net/";

        public static string APIServerUrl => settings.APIServerLocation + ":" + settings.APIServerPort;
        public static string InfoFilePath => APIServerUrl + "/" + InfoFilename;
        public static string IndexFilePath => APIServerUrl + "/" + IndexFilename;
        public static string LauncherConfigPath => APIServerUrl + "/" + LauncherConfigFilename;
        public static string PatchFolderPath => APIServerUrl + "/" + ResourceFolderName;
        public static string SkinFolderPath => APIServerUrl + "/" + SkinFolderName;
    }
}