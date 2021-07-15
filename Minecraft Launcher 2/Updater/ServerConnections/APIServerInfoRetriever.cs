using Minecraft_Launcher_2.Updater;
using Minecraft_Launcher_2.Updater.ServerConnections;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.ServerConnections
{
    public class APIServerInfoRetriever
    {
        public const int Timeout = 2000;
        private ConnectionState _conState = new ConnectionState { State = RetrieveState.Processing };

        public event EventHandler<ConnectionState> OnConnectionStateChanged;

        public async Task RetrieveFromAPIServer()
        {
            try
            {
                ConnectionState = new ConnectionState { State = RetrieveState.Processing };

                bool isActive = await CommonUtils.IsActiveHttpServer(URLs.InfoFile);
                if (!isActive)
                    throw new Exception("API 서버와 연결할 수 없습니다.");

                using (TimeoutWebClient client = new TimeoutWebClient(Timeout))
                {
                    string data = await client.DownloadStringTaskAsync(URLs.InfoFile);
                    JObject obj = JObject.Parse(data);
                    PatchVersion = obj.Value<string>("patchVersion");
                    Notice = obj.Value<string>("notice");
                }

                ConnectionState = new ConnectionState { State = RetrieveState.Loaded };
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ConnectionState = new ConnectionState { State = RetrieveState.Error, ErrorMessage = "API 서버: " + e.Message };
            }
        }


        public ConnectionState ConnectionState
        {
            get => _conState;
            set
            {
                _conState = value;
                OnConnectionStateChanged?.Invoke(this, value);
            }
        }

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
