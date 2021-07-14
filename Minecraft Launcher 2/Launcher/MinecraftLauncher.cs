using Minecraft_Launcher_2.Updater.ServerConnections;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minecraft_Launcher_2.Launcher
{
    internal class LaunchSetting
    {
        private readonly ServerDataContext _context;
        public string MainClass { get; private set; }
        public string MinecraftArguments { get; private set; }
        public string AssetsVersion { get; private set; }
        public string MinecraftVersion { get; private set; }
        public List<string> Libraries { get; private set; } = new List<string>();

        internal LaunchSetting(ServerDataContext context)
        {
            _context = context;
        }

        public void Load(string settingFile)
        {
            string data = File.ReadAllText(settingFile);
            JObject json = JObject.Parse(data);

            _context.ReadInstalledPatchVersion();
            MainClass = json.Value<string>("mainClass");
            MinecraftArguments = json.Value<string>("arguments");
            AssetsVersion = json.Value<string>("assets");
            MinecraftVersion = _context.InstalledVersion.Split('@')[0];

            JArray libs = json["libraries"] as JArray;
            foreach (JObject obj in libs)
            {
                string[] names = obj.Value<string>("name").Split(':');
                string jarfile = string.Format("{0}\\{1}\\{2}\\{1}-{2}.jar", names[0].Replace('.', '\\'), names[1], names[2]);
                Libraries.Add(Path.Combine(Properties.Settings.Default.MinecraftDir, "libraries", jarfile));
            }
        }
    }

    public class MinecraftLauncher
    {
        private static readonly Properties.Settings settings = Properties.Settings.Default;
        private volatile bool _isRunning = false;

        public event EventHandler<string> OnLog;
        public event EventHandler<string> OnError;
        public event EventHandler<int> OnExited;

        public ServerDataContext Context { get; }

        public MinecraftLauncher(ServerDataContext context)
        {
            Context = context;
        }


        private string GetLaunchAdditionalArguments(LaunchSetting launchSettings)
        {
            string arg = launchSettings.MinecraftArguments;
            arg = arg.Replace("${auth_player_name}", PlayerName);
            arg = arg.Replace("${version_name}", launchSettings.MinecraftVersion);
            arg = arg.Replace("${game_directory}", settings.MinecraftDir);
            arg = arg.Replace("${assets_root}", Path.Combine(settings.MinecraftDir, "assets"));
            arg = arg.Replace("${assets_index_name}", launchSettings.AssetsVersion);
            arg = arg.Replace("${auth_uuid}", "sessionid");
            arg = arg.Replace("${auth_access_token}", "-");
            arg = arg.Replace("${user_type}", "-");
            return arg;
        }

        private string GetArguments()
        {
            Log("Extracting launcher info....");
            LaunchSetting launchSettings = new LaunchSetting(Context);
            launchSettings.Load(Path.Combine(settings.MinecraftDir, "launch-config.json"));

            Log("Building arguments....");

            StringBuilder sb = new StringBuilder();
            sb.Append("-Xmx");
            sb.Append(settings.MemorySize);
            sb.Append("G ");
            sb.Append("-Djava.library.path=");
            sb.Append(Path.Combine(settings.MinecraftDir, "natives"));
            sb.Append(" -cp ");

            foreach (string lib in launchSettings.Libraries)
            {
                sb.Append(Path.Combine(settings.MinecraftDir, "libraries", lib));
                sb.Append(';');
            }

            sb.Append(Path.Combine(settings.MinecraftDir, "minecraft.jar"));
            sb.Append(' ');
            sb.Append(launchSettings.MainClass);
            sb.Append(' ');
            sb.Append(GetLaunchAdditionalArguments(launchSettings));

            if (Context.Retriever.ConnectionState.State == RetrieveState.Loaded && settings.AutoJoinServer)
            {
                sb.Append(" --server ");
                sb.Append(settings.MinecraftServerIP);
                sb.Append(" --port ");
                sb.Append(settings.MinecraftServerPort);
            }

            return sb.ToString();
        }

        public Task Start()
        {
            return Task.Factory.StartNew(StartSync);
        }

        private void StartSync()
        {
            _isRunning = true;
            try
            {
                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = "java";
                info.Arguments = GetArguments();
                info.WorkingDirectory = settings.MinecraftDir;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;

                if (settings.UseLogging)
                {
                    info.RedirectStandardOutput = true;
                    info.RedirectStandardError = true;

                    p.OutputDataReceived += (sender, ar) => Log(ar.Data);
                    p.ErrorDataReceived += (sender, ar) => Error(ar.Data);
                }

                Log("Starting process...");

                p.StartInfo = info;
                p.Start();

                if (settings.UseLogging)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    p.WaitForExit();
                    OnExited?.Invoke(this, p.ExitCode);
                    _isRunning = false;
                }
                else
                    Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown(0));
            }
            catch (Exception e)
            {
                Error(e.ToString());
            }
        }

        private void Log(string str)
        {
            OnLog?.Invoke(this, str);
        }

        private void Error(string str)
        {
            OnError?.Invoke(this, str);
        }


        public string PlayerName { get; set; } = "Unnamed";

        public bool IsRunning => _isRunning;
    }
}
