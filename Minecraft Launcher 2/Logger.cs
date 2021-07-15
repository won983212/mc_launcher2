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

        public static void Info(object data)
        {
            AppendLog(LogType.Info, data);
        }

        public static void Debug(object data)
        {
            AppendLog(LogType.Debug, data);
        }

        public static void Error(object data)
        {
            AppendLog(LogType.Error, data);
        }

        private static void AppendLog(LogType type, object data)
        {
            Console.WriteLine("[" + type.ToString() + "] " + (data ?? "null"));
            App.Current.Dispatcher.Invoke(() =>
            {
                _logs.Add(new LogMessage(type, (data ?? "null").ToString()));
            });
        }

        public static void ClearLogs()
        {
            _logs.Clear();
        }

        public static IEnumerable<LogMessage> Logs => _logs;
    }
}
