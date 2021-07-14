using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater;
using System.IO;

namespace Minecraft_Launcher_2.Launcher
{
    public enum LauncherState
    {
        NeedInstall, NeedUpdate, CanStart, Offline
    }

    public class LauncherContext
    {
        public ServerInfoRetriever Retriever { get; private set; } = new ServerInfoRetriever();
        public string InstalledVersion { get; private set; }

        public LauncherContext()
        { }

        public void GetInstalledPatchVersion()
        {
            string filePath = Path.Combine(Properties.Settings.Default.MinecraftDir, "version");
            InstalledVersion = File.Exists(filePath) ? File.ReadAllText(filePath) : "Unknown";
        }

        public LauncherState GetLauncherState()
        {
            GetInstalledPatchVersion();
            if (Retriever.ConnectionState.State == RetrieveState.Error)
                return LauncherState.Offline;
            else if (InstalledVersion == "Unknown")
                return LauncherState.NeedInstall;
            else if (InstalledVersion != Retriever.ResourceServerData.PatchVersion)
                return LauncherState.NeedUpdate;

            return LauncherState.CanStart;
        }
    }
}
