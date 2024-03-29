﻿using Minecraft_Launcher_2.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Launcher
{
    internal class LaunchSetting
    {
        private readonly ServerDataContext _context;

        internal LaunchSetting(ServerDataContext context)
        {
            _context = context;
        }

        public LaunchConfigContext LaunchConfig { get; private set; }

        public string MinecraftVersion { get; private set; }

        public void Load(string settingFile)
        {
            var data = File.ReadAllText(settingFile);
            _context.ReadInstalledPatchVersion();
            LaunchConfig = new LaunchConfigContext(JObject.Parse(data));
            MinecraftVersion = _context.InstalledVersion.Split('@')[0];
        }
    }

    public class MinecraftLauncher
    {
        private static readonly Settings settings = Settings.Default;
        private volatile bool _isRunning;


        public MinecraftLauncher(ServerDataContext context)
        {
            Context = context;
        }


        public string PlayerName { get; set; } = "Unnamed";

        public bool IsRunning => _isRunning;

        public ServerDataContext Context { get; }

        public bool IsAutoJoin { get; set; }

        public event EventHandler<string> OnLog;
        public event EventHandler<string> OnError;
        public event EventHandler<int> OnExited;


        private string GetParsedArguments(string arg, LaunchSetting launchSettings)
        {
            arg = arg.Replace("${auth_player_name}", PlayerName);
            arg = arg.Replace("${version_name}", launchSettings.MinecraftVersion);
            arg = arg.Replace("${game_directory}", settings.MinecraftDir);
            arg = arg.Replace("${assets_root}", Path.Combine(settings.MinecraftDir, "assets"));
            arg = arg.Replace("${assets_index_name}", launchSettings.LaunchConfig.AssetsVersion);
            arg = arg.Replace("${auth_uuid}", "sessionid");
            arg = arg.Replace("${auth_access_token}", "-");
            arg = arg.Replace("${user_type}", "mojang");
            arg = arg.Replace("${version_type}", "release");
            arg = arg.Replace("${natives_directory}", Path.Combine(settings.MinecraftDir, "natives"));
            arg = arg.Replace("${launcher_name}", "loot-launcher");
            arg = arg.Replace("${launcher_version}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            return arg;
        }

        private string GetArguments()
        {
            Log("Extracting launcher info....");
            var launchSettings = new LaunchSetting(Context);
            launchSettings.Load(Path.Combine(settings.MinecraftDir, "launch-config.json"));

            Log("Building arguments....");

            var sb = new StringBuilder();
            sb.Append('\"');
            foreach (var lib in launchSettings.LaunchConfig.Libraries)
            {
                sb.Append(lib.GetPath());
                sb.Append(';');
            }

            sb.Append(Path.Combine(settings.MinecraftDir, "minecraft.jar"));
            sb.Append('\"');

            var classpath = sb.ToString();
            sb = new StringBuilder();
            sb.Append("-Xmx");
            sb.Append(settings.MemorySize);
            sb.Append("G ");
            sb.Append(GetParsedArguments(launchSettings.LaunchConfig.MinecraftJVMArguments, launchSettings)
                .Replace("${classpath}", classpath));
            sb.Append(' ');
            sb.Append(launchSettings.LaunchConfig.MainClass);
            sb.Append(' ');
            sb.Append(GetParsedArguments(launchSettings.LaunchConfig.MinecraftGameArguments, launchSettings));

            if (IsAutoJoin)
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
                var p = new Process();
                var info = new ProcessStartInfo();

                info.FileName = settings.JavaCommand;
                info.Arguments = GetArguments();
                info.WorkingDirectory = settings.MinecraftDir;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;

                Log("Argument: " + info.Arguments);

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
    }
}