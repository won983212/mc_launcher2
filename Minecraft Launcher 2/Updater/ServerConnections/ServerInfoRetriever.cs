using Minecraft_Launcher_2.Updater.ServerConnections;
using System;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.ServerConnections
{
    public class ServerInfoRetriever
    {
        public const int Timeout = 3000;
        private ConnectionState _conState = new ConnectionState { State = RetrieveState.Processing };
        private ResourceServerConnection _resourceServer = new ResourceServerConnection();
        private MinecraftServerConnection _minecraftServer = new MinecraftServerConnection();

        public event EventHandler<ConnectionState> OnConnectionStateChanged;

        public async Task RetrieveAll()
        {
            string messagePrefix = "API 서버: ";

            try
            {
                ConnectionState = new ConnectionState { State = RetrieveState.Processing };
                ResourceServerData = await _resourceServer.RetrieveInfoAsync();
                messagePrefix = "Minecraft 서버: ";
                MinecraftServerData = await _minecraftServer.RetrieveServerStatusAsync();
                ConnectionState = new ConnectionState { State = RetrieveState.Loaded };
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ConnectionState = new ConnectionState { State = RetrieveState.Error, ErrorMessage = messagePrefix + e.Message };
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

        public ResourceInfo ResourceServerData { get; private set; } = new ResourceInfo();

        public MinecraftServerInfo MinecraftServerData { get; private set; } = new MinecraftServerInfo();
    }
}
