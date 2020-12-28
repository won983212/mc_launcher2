using Minecraft_Launcher_2.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    public enum LauncherState
    {
        NeedInstall, NeedUpdate, CanStart, Offline
    }

    public class LauncherContext
    {
        public ServerStatus ServerStatus { get; private set; } = new ServerStatus();
        public string PatchVersion { get; private set; }

        public LauncherContext()
        {
            UpdatePatchVersion();
            ServerStatus.RetrieveAll();
        }

        public void UpdatePatchVersion()
        {
            string filePath = Path.Combine(Properties.Settings.Default.MinecraftDir, "version");
            if (File.Exists(filePath))
            {
                PatchVersion = File.ReadAllText(filePath);
            }
            else
            {
                PatchVersion = "Unknown";
            }
        }

        public LauncherState GetLauncherState()
        {
            if(ServerStatus.ConnectionState.State == RetrieveState.Error)
            {
                return LauncherState.Offline;
            } 
            else if (PatchVersion == "Unknown")
            {
                return LauncherState.NeedInstall;
            }
            else if (PatchVersion != ServerStatus.PatchVersion)
            {
                return LauncherState.NeedUpdate;
            }

            return LauncherState.CanStart;
        }
    }
}
