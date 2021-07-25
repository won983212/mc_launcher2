using System.IO;
using Minecraft_Launcher_2.Properties;
using Minecraft_Launcher_2.ServerConnections;
using Minecraft_Launcher_2.Updater.ServerConnections;

namespace Minecraft_Launcher_2.Launcher
{
    public enum LauncherState
    {
        NeedInstall,
        NeedUpdate,
        CanStart,
        Offline
    }

    public class ServerDataContext
    {
        public APIServerInfoRetriever Retriever { get; } = new APIServerInfoRetriever();
        public string InstalledVersion { get; private set; }

        public void ReadInstalledPatchVersion()
        {
            var filePath = Path.Combine(Settings.Default.MinecraftDir, "version");
            InstalledVersion = File.Exists(filePath) ? File.ReadAllText(filePath) : "Unknown";
        }

        public LauncherState GetLauncherState()
        {
            ReadInstalledPatchVersion();
            if (Retriever.ConnectionState.State == RetrieveState.Error)
                return LauncherState.Offline;
            if (InstalledVersion == "Unknown")
                return LauncherState.NeedInstall;
            if (InstalledVersion != Retriever.PatchVersion)
                return LauncherState.NeedUpdate;

            return LauncherState.CanStart;
        }
    }
}