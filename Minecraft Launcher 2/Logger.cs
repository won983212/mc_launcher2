using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Minecraft_Launcher_2
{
    public enum LogType
    {
        Info, Error, Debug
    }

    public class LogMessage
    {
        public DateTime Time { get; }
        public LogType Type { get; }
        public string Message { get; }

        internal LogMessage(LogType type, string message)
        {
            Time = DateTime.Now;
            Type = type;
            Message = message;
        }
    }

    public static class Logger
    {
        private static readonly ObservableCollection<LogMessage> _logs = new ObservableCollection<LogMessage>();

        public static void Log(object data)
        {
            Console.WriteLine("[Info] " + data);
            _logs.Add(new LogMessage(LogType.Info, data.ToString()));
        }

        public static void Debug(object data)
        {
            if (data == null)
                Console.WriteLine();
            else
                Console.WriteLine("[Debug] " + data);
            _logs.Add(new LogMessage(LogType.Debug, data.ToString()));
        }

        public static void Error(object data)
        {
            Console.WriteLine("[Error] " + data);
            _logs.Add(new LogMessage(LogType.Error, data.ToString()));
        }

        public static void ClearLogs()
        {
            _logs.Clear();
        }

        public static IEnumerable<LogMessage> Logs => _logs;
    }
}
