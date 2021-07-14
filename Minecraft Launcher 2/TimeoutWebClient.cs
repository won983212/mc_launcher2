using System;
using System.Net;

namespace Minecraft_Launcher_2
{
    class TimeoutWebClient : WebClient
    {
        public int Timeout { get; set; } = 10000;

        public TimeoutWebClient()
        {
        }

        public TimeoutWebClient(int timeout)
        {
            Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);
            req.Timeout = Timeout;
            return req;
        }
    }
}
