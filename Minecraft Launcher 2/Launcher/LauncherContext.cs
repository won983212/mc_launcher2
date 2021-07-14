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
        public ServerInfoRetriever ServerStatus { get; private set; } = new ServerInfoRetriever();
        public string PatchVersion { get; private set; }

        public LauncherContext()
        { }

        public void GetInstalledPatchVersion()
        {
            string filePath = Path.Combine(Properties.Settings.Default.MinecraftDir, "version");
            PatchVersion = File.Exists(filePath) ? File.ReadAllText(filePath) : "Unknown";
        }

        public LauncherState GetLauncherState()
        {
            GetInstalledPatchVersion();
            if (ServerStatus.ConnectionState.State == RetrieveState.Error)
                return LauncherState.Offline;
            else if (PatchVersion == "Unknown")
                return LauncherState.NeedInstall;
            else if (PatchVersion != ServerStatus.ResourceServerData.PatchVersion)
                return LauncherState.NeedUpdate;

            return LauncherState.CanStart;
        }
    }
}
