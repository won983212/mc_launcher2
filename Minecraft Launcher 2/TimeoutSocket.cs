using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
