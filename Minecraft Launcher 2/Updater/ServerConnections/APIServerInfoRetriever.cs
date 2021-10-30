using Minecraft_Launcher_2.Updater;
using Minecraft_Launcher_2.Updater.ServerConnections;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.ServerConnections
{
    public class APIServerInfoRetriever
    {
        public const int Timeout = 2000;
        private ConnectionState _conState = new ConnectionState { State = RetrieveState.Processing };


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

        public event EventHandler<ConnectionState> OnConnectionStateChanged;

        public async Task RetrieveFromAPIServer()
        {
            try
            {
                ConnectionState = new ConnectionState { State = RetrieveState.Processing };

                var isActive = await CommonUtils.IsActiveHttpServer(URLs.InfoFilePath);
                if (!isActive)
                    throw new Exception("API 서버와 연결할 수 없습니다.");

                using (var client = new TimeoutWebClient(Timeout))
                {
                    var data = await client.DownloadStringTaskAsync(URLs.InfoFilePath);
                    var obj = JObject.Parse(data);
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

        public bool RetrieveFromAPIServerDirectory(string path)
        {
            string infoFilePath = Path.Combine(path, URLs.InfoFilename);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(infoFilePath))
            {
                try
                {
                    var json = JObject.Parse(File.ReadAllText(infoFilePath));
                    PatchVersion = json.Value<string>("patchVersion");
                    Notice = json.Value<string>("notice");

                    if (PatchVersion == null || Notice == null)
                        throw new ArgumentException("서버로 부터 받은 json을 파싱할 수 없습니다.");

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            return false;
        }
    }

    internal class TimeoutWebClient : WebClient
    {
        public TimeoutWebClient()
        {
            Encoding = Encoding.UTF8;
        }

        public TimeoutWebClient(int timeout) : this()
        {
            Timeout = timeout;
        }

        public int Timeout { get; set; } = 10000;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var req = base.GetWebRequest(address);
            req.Timeout = Timeout;
            return req;
        }
    }
}