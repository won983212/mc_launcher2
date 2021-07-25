using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Minecraft_Launcher_2
{
    public enum LogType
    {
        Info,
        Error,
        Debug
    }

    public class LogMessage
    {
        internal LogMessage(LogType type, string message)
        {
            Time = DateTime.Now;
            Type = type;
            Message = message;
        }

        public DateTime Time { get; }
        public LogType Type { get; }
        public string Message { get; }
    }

    public static class Logger
    {
        private static readonly ObservableCollection<LogMessage> _logs = new ObservableCollection<LogMessage>();

        public static IEnumerable<LogMessage> Logs => _logs;

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
            Console.WriteLine("[" + type + "] " + (data ?? "null"));
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logs.Add(new LogMessage(type, (data ?? "null").ToString()));
            });
        }

        public static void ClearLogs()
        {
            _logs.Clear();
        }
    }
}