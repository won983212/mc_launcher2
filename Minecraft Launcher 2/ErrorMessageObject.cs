using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
