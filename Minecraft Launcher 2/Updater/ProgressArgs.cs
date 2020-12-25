using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
    public class ProgressArgs
    {
        public double Progress { get; }
        public string Status { get; }

        public ProgressArgs(double progress, string status)
        {
            Progress = progress;
            Status = status;
        }
    }
}
