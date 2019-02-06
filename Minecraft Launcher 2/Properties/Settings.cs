using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Properties
{
	internal sealed partial class Settings
	{
		public Settings()
		{
			SettingsLoaded += Settings_SettingsLoaded;
		}

		private void Settings_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
		{
			Uri result;
			if (!Uri.TryCreate(Default.UpdateHost, UriKind.Absolute, out result) || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
			{
				Default.UpdateHost = "http://localhost/dataserver";
				Default.Save();
			}

			if (!Directory.Exists(Default.Minecraft_Dir))
			{
				Default.Minecraft_Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "minecraft_data");
				Default.Save();
			}
		}
	}
}
