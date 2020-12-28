using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
    public class ServerStatus
    {
        private const int Timeout = 3000;
        private MemoryStream ms = new MemoryStream();
        private ConnectionState _conState = new ConnectionState { State = RetrieveState.Processing };

        // minecraft server status
        public string Motd { get; private set; }
        public int PlayersOnline { get; private set; }
        public int PlayersMax { get; private set; }
        public int Protocol { get; private set; }
        public int Ping { get; private set; }

        // api server status
        public string Notice { get; private set; }
        public string PatchVersion { get; private set; }
        public string ClientVersion { get; private set; }

        public ConnectionState ConnectionState
        {
            get => _conState;
            set
            {
                _conState = value;
                OnConnectionStateChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<ConnectionState> OnConnectionStateChanged;

        public async Task RetrieveAll()
        {
            string messagePrefix = "API 서버: ";

            try
            {
                ConnectionState = new ConnectionState { State = RetrieveState.Processing };
                await _RetrieveAPIServerVersion();
                messagePrefix = "Minecraft 서버: ";
                await _RetrieveServerStatus();
                ConnectionState = new ConnectionState { State = RetrieveState.Loaded };
            }
            catch (Exception e)
            {
                Logger.Debug(e);
                ConnectionState = new ConnectionState { State = RetrieveState.Error, ErrorMessage = messagePrefix + e.Message };
            }
        }

        public static bool IsActiveAPIServer()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URLs.InfoFile);
                req.Timeout = Timeout;
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

        public async Task RetrieveAPIServerVersion()
        {
            try
            {
                ConnectionState = new ConnectionState { State = RetrieveState.Processing };
                await _RetrieveAPIServerVersion();
                ConnectionState = new ConnectionState { State = RetrieveState.Loaded };
            }
            catch (Exception e)
            {
                Logger.Debug(e);
                ConnectionState = new ConnectionState { State = RetrieveState.Error, ErrorMessage = "API 서버: " + e.Message };
            }
        }

        private async Task _RetrieveAPIServerVersion()
        {
            bool isActive = await Task.Factory.StartNew(IsActiveAPIServer);
            if (!isActive)
                throw new Exception("API 서버와 연결할 수 없습니다.");

            using (TimeoutWebClient client = new TimeoutWebClient(Timeout))
            {
                string data = await client.DownloadStringTaskAsync(URLs.InfoFile);
                JObject obj = JObject.Parse(data);
                PatchVersion = (string)obj["patchVersion"];
                ClientVersion = (string)obj["clientVersion"];

                Notice = await client.DownloadStringTaskAsync(URLs.Notice);
            }
        }

        public async Task RetrieveServerStatus()
        {
            try
            {
                ConnectionState = new ConnectionState { State = RetrieveState.Processing };
                await _RetrieveServerStatus();
                ConnectionState = new ConnectionState { State = RetrieveState.Loaded };
            } 
            catch(Exception e)
            {
                Logger.Debug(e);
                ConnectionState = new ConnectionState { State = RetrieveState.Error, ErrorMessage = "Minecraft 서버: " + e.Message };
            }
        }

        private async Task _RetrieveServerStatus()
        {
            Tuple<string, int> data = await Task.Factory.StartNew(RetrieveServerStatusSync);

            if (data.Item2 == 0x19)
                throw new InvalidDataException(data.Item1);

            JObject json = JObject.Parse(data.Item1);
            Motd = (string)json["description"]["text"];
            PlayersOnline = (int)json["players"]["online"];
            PlayersMax = (int)json["players"]["max"];
            Protocol = (int)json["version"]["protocol"];

            Logger.Debug("[ServerStatus] Server status has retrieved");
        }

        private Tuple<string, int> RetrieveServerStatusSync()
        {
            string serverIP = Properties.Settings.Default.MinecraftServerIP;
            int serverPort = Properties.Settings.Default.MinecraftServerPort;

            TcpClient client = new TcpClient();
            TimeoutSocket.Connect(client, serverIP, serverPort, Timeout);

            Logger.Debug("[ServerStatus] Connected to " + serverIP);
            BufferedStream stream = new BufferedStream(client.GetStream());

            // handshake
            BinaryWriter bw = new BinaryWriter(stream);
            WriteVarInt(ms, -1);
            WriteString(ms, serverIP);
            WriteUnsignedShort(ms, serverPort);
            WriteVarInt(ms, 1);
            Flush(bw, 0);
            Flush(bw, 0);

            BinaryReader br = new BinaryReader(stream);
            int len = ReadVarInt(br); // content-length
            int id = ReadVarInt(br); // id
            string data = ReadString(br);

            // ping pong
            long ticks = DateTime.UtcNow.Ticks;
            WriteLong(ms, ticks);
            Flush(bw, 1);

            len = ReadVarInt(br); // content-length
            id = ReadVarInt(br); // id
            long pong = ReadLong(br);

            Ping = (int) ((DateTime.UtcNow.Ticks - pong) / 10000.0);
            Logger.Debug("[ServerStatus] Pong! " + Ping);

            client.Close();
            return new Tuple<string, int>(data, id);
        }

        private void Flush(BinaryWriter bw, int id)
        {
            byte[] data = ms.ToArray();
            ms.SetLength(0);

            int idLen = WriteVarInt(ms, id);
            byte[] idData = ms.ToArray();
            ms.SetLength(0);

            WriteVarInt(bw.BaseStream, data.Length + idLen);
            bw.Write(idData, 0, idData.Length);
            bw.Write(data, 0, data.Length);
            bw.Flush();
        }

        private int ReadVarInt(BinaryReader br)
        {
            var value = 0;
            var size = 0;
            byte b;
            while ((((b = br.ReadByte()) & 0x80) == 0x80))
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                    throw new IOException("This VarInt is too big!");
            }
            return value | ((b & 0x7F) << (size * 7));
        }

        private string ReadString(BinaryReader br)
        {
            int size = ReadVarInt(br);
            byte[] data = br.ReadBytes(size);
            return Encoding.UTF8.GetString(data);
        }

        private long ReadLong(BinaryReader br)
        {
            long data = 0;
            for (int i = 0; i < 8; i++)
            {
                data |= br.ReadByte();
                if(i != 7) data <<= 8;
            }
            return data;
        }

        private void WriteString(Stream stream, string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            WriteVarInt(stream, data.Length);
            stream.Write(data, 0, data.Length);
        }

        private int WriteVarInt(Stream stream, int value)
        {
            int size = 1;
            while((value & -128) != 0)
            {
                stream.WriteByte((byte)(value & 127 | 128));
                value = (byte)(((uint)value) >> 7);
                size++;
            }
            stream.WriteByte((byte) value);
            return size;
        }

        private void WriteUnsignedShort(Stream stream, int value)
        {
            stream.WriteByte((byte)((value >> 8) & 0xff));
            stream.WriteByte((byte)(value & 0xff));
        }

        private void WriteLong(Stream stream, long value)
        {
            for(int i = 7; i >= 0; i--)
                stream.WriteByte((byte)((value >> (8 * i)) & 0xff));
        }
    }
}
