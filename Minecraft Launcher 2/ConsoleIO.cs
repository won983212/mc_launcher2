using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2
{
    public interface ConsoleIO
    {
		void Info(string text);
		void Error(string text);
	}
}
