using System;

namespace Minecraft_Launcher_2
{
    public class ErrorMessageObject
    {
        public string Title { get; set; }

        public string Message { get; set; }

        public string FullMessage { get; set; }

        public Action Callback { get; set; }
    }
}
