﻿using System;
using System.IO;

namespace Minecraft_Launcher_2.Properties
{
    internal sealed partial class Settings
    {
        public Settings()
        {
            SettingsLoaded += Settings_SettingsLoaded;
            SettingsSaving += Settings_SettingsSaving;
        }

        private void Settings_SettingsSaving(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DetectWrong();
        }

        private void Settings_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            if (DetectWrong())
                Default.Save();
        }

        private bool DetectWrong()
        {
            bool modified = false;

            if (!Uri.TryCreate(Default.APIServerLocation, UriKind.Absolute, out Uri result) || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
            {
                Default.APIServerLocation = "http://localhost";
                modified = true;
            }

            if (!Directory.Exists(Default.MinecraftDir))
            {
                string folderName = "minecraft_" + Default.FolderName.ToLower();
                Default.MinecraftDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName);
                modified = true;
            }

            string mcdir = Default.MinecraftDir;
            if (mcdir.EndsWith("/") || mcdir.EndsWith("\\"))
            {
                Default.MinecraftDir = mcdir.Substring(0, mcdir.Length - 1);
                modified = true;
            }

            return modified;
        }
    }
}
