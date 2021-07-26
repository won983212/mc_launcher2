using Minecraft_Launcher_2.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.ServerConnections
{
    public class MinecraftServerConnection
    {
        private readonly MemoryStream ms = new MemoryStream();

        public async Task<MinecraftServerInfo> RetrieveServerStatusAsync()
        {
            var model = new MinecraftServerInfo();
            var data = await Task.Factory.StartNew(() => RetrieveServerStatusSync(model));

            if (data.Item2 == -1)
                throw new InvalidDataException(data.Item1);

            if (data.Item2 == 0x19)
                throw new InvalidDataException(data.Item1);

            var json = JObject.Parse(data.Item1);
            if (!json.ContainsKey("description"))
                throw new InvalidDataException("서버는 열려있지만 아직 로딩중입니다. 잠시후에 다시 시도해보세요. (JSON: " + json + ")");

            model.Motd = (string)json["description"]["text"];
            model.PlayersOnline = (int)json["players"]["online"];
            model.PlayersMax = (int)json["players"]["max"];
            model.Protocol = (int)json["version"]["protocol"];

            Logger.Debug("[ServerStatus] Server status has retrieved");
            return model;
        }

        private Tuple<string, int> RetrieveServerStatusSync(MinecraftServerInfo infoHolder)
        {
            var serverIP = Settings.Default.MinecraftServerIP;
            var serverPort = Settings.Default.MinecraftServerPort;

            var client = new TcpClient();
            try
            {
                Connect(client, serverIP, serverPort, APIServerInfoRetriever.Timeout);
            }
            catch (SocketException e)
            {
                return new Tuple<string, int>("연결할 수 없습니다: " + e.Message, -1);
            }

            Logger.Debug("[ServerStatus] Connected to " + serverIP);
            var stream = new BufferedStream(client.GetStream());

            // handshake
            var bw = new BinaryWriter(stream);
            WriteVarInt(ms, -1);
            WriteString(ms, serverIP);
            WriteUnsignedShort(ms, serverPort);
            WriteVarInt(ms, 1);
            Flush(bw, 0);
            Flush(bw, 0);

            var br = new BinaryReader(stream);
            var len = ReadVarInt(br); // content-length
            var id = ReadVarInt(br); // id
            var data = ReadString(br);

            // ping pong
            var ticks = DateTime.UtcNow.Ticks;
            WriteLong(ms, ticks);
            Flush(bw, 1);

            len = ReadVarInt(br); // content-length
            id = ReadVarInt(br); // id
            var pong = ReadLong(br);

            infoHolder.Ping = (int)((DateTime.UtcNow.Ticks - pong) / 10000.0);
            Logger.Debug("[ServerStatus] Pong! " + infoHolder.Ping);

            client.Close();
            return new Tuple<string, int>(data, id);
        }

        private void Flush(BinaryWriter bw, int id)
        {
            var data = ms.ToArray();
            ms.SetLength(0);

            var idLen = WriteVarInt(ms, id);
            var idData = ms.ToArray();
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
            while (((b = br.ReadByte()) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                    throw new IOException("This VarInt is too big!");
            }

            return value | ((b & 0x7F) << (size * 7));
        }

        private string ReadString(BinaryReader br)
        {
            var size = ReadVarInt(br);
            var data = br.ReadBytes(size);
            return Encoding.UTF8.GetString(data);
        }

        private long ReadLong(BinaryReader br)
        {
            long data = 0;
            for (var i = 0; i < 8; i++)
            {
                data |= br.ReadByte();
                if (i != 7) data <<= 8;
            }

            return data;
        }

        private void WriteString(Stream stream, string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            WriteVarInt(stream, data.Length);
            stream.Write(data, 0, data.Length);
        }

        private int WriteVarInt(Stream stream, int value)
        {
            var size = 1;
            while ((value & -128) != 0)
            {
                stream.WriteByte((byte)((value & 127) | 128));
                value = (byte)((uint)value >> 7);
                size++;
            }

            stream.WriteByte((byte)value);
            return size;
        }

        private void WriteUnsignedShort(Stream stream, int value)
        {
            stream.WriteByte((byte)((value >> 8) & 0xff));
            stream.WriteByte((byte)(value & 0xff));
        }

        private void WriteLong(Stream stream, long value)
        {
            for (var i = 7; i >= 0; i--)
                stream.WriteByte((byte)((value >> (8 * i)) & 0xff));
        }

        private static void Connect(TcpClient client, string hostname, int port, int timeout)
        {
            var res = client.BeginConnect(hostname, port, null, null);
            var success = res.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
                client.EndConnect(res);
            else
            {
                client.Close();
                throw new SocketException(10060);
            }
        }
    }

    public class MinecraftServerInfo
    {
        public string Motd { get; internal set; }
        public int PlayersOnline { get; internal set; }
        public int PlayersMax { get; internal set; }
        public int Protocol { get; internal set; }
        public int Ping { get; internal set; }
    }
}