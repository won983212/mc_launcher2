using Minecraft_Launcher_2.Updater;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.ServerConnections
{
    internal class ResourceServerConnection
    {
        public async Task<ResourceInfo> RetrieveInfoAsync()
        {
            bool isActive = await Task.Factory.StartNew(IsActiveAPIServer);
            if (!isActive)
                throw new Exception("API 서버와 연결할 수 없습니다.");

            ResourceInfo model = new ResourceInfo();
            using (TimeoutWebClient client = new TimeoutWebClient(ServerInfoRetriever.Timeout))
            {
                string data = await client.DownloadStringTaskAsync(URLs.InfoFile);
                JObject obj = JObject.Parse(data);
                model.PatchVersion = obj.Value<string>("patchVersion");
                model.Notice = obj.Value<string>("notice");
            }

            return model;
        }

        private static bool IsActiveAPIServer()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URLs.InfoFile);
                req.Timeout = ServerInfoRetriever.Timeout;
                req.AllowAutoRedirect = false;
                req.Method = "HEAD";
                req.GetResponse().Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ResourceInfo
    {
        public string Notice { get; internal set; }
        public string PatchVersion { get; internal set; }
    }

    internal class TimeoutWebClient : WebClient
    {
        public int Timeout { get; set; } = 10000;

        public TimeoutWebClient()
        {
            Encoding = System.Text.Encoding.UTF8;
        }

        public TimeoutWebClient(int timeout) : this()
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
