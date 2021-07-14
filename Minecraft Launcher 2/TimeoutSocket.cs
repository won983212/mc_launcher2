using System;
using System.Net.Sockets;

namespace Minecraft_Launcher_2
{
    class TimeoutSocket
    {
        public static void Connect(TcpClient client, string hostname, int port, int timeout)
        {
            IAsyncResult res = client.BeginConnect(hostname, port, null, null);
            bool success = res.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
                client.EndConnect(res);
            else
            {
                client.Close();
                throw new SocketException(10060);
            }
        }
    }
}
