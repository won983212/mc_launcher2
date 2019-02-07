using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
	public class TimeoutWebClient : WebClient
	{
		public int Timeout { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest w = base.GetWebRequest(address);
			w.Timeout = Timeout;
			((HttpWebRequest)w).ReadWriteTimeout = Timeout;
			return w;
		}
	}
}
