using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    // TODO 향후 log / debug를 따로 콘솔창 만들어서 사용
    public static class Logger
    {
        public static void Log(object data)
        {
            Console.WriteLine("[Log] " + data);
        }

        public static void Debug(object data)
        {
#if DEBUG
            if (data == null)
                Console.WriteLine();
            else
                Console.WriteLine("[DEBUG] " + data);
#endif
        }

        public static void Error(object data)
        {
            Console.WriteLine("[Error] " + data);
        }
    }
}
