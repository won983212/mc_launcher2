namespace Minecraft_Launcher_2.Updater
{
    public static class URLs
    {
        private static Properties.Settings settings = Properties.Settings.Default;

        public static string InfoFile
        {
            get
            {
                return settings.APIServerLocation + "/info.json";
            }
        }

        public static string IndexFile
        {
            get
            {
                return settings.APIServerLocation + "/indexes.json";
            }
        }

        public static string PatchFolder
        {
            get
            {
                return settings.APIServerLocation + "/resources";
            }
        }

        public static string LauncherConfig
        {
            get
            {
                return settings.APIServerLocation + "/launch-config.json";
            }
        }

        public static string APIUserInfo(string uuid)
        {
            return settings.APIServerLocation + "/api/user/" + uuid;
        }

        public static readonly string AssetsURL = "http://resources.download.minecraft.net/";
    }
}
