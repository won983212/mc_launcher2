using System;

namespace Minecraft_Launcher_2
{
    public static class Logger
    {
        public static void Log(object data)
        {
            if (App.Console != null)
                App.Console.Info("[Info] " + data);
            Console.WriteLine("[Info] " + data);
        }

        public static void Debug(object data)
        {
            if (data == null)
                Console.WriteLine();
            else
                Console.WriteLine("[Debug] " + data);
            if (App.Console != null)
                App.Console.Info("[Debug] " + data);
        }

        public static void Error(object data)
        {
            Console.WriteLine("[Error] " + data);
            if (App.Console != null)
                App.Console.Error("[Error] " + data);
        }
    }
}
