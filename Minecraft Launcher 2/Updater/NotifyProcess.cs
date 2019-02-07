using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
	class NotifyProcess
	{
		public event EventHandler<Tuple<double, string>> CurrentDownloadProgress;
		public event EventHandler<Tuple<double, string>> TotalDownloadProgress;
		public event EventHandler<int> OnComplete;
		public event EventHandler<Exception> OnException;

		protected void FireOnComplete(int failed)
		{
			OnComplete?.Invoke(this, failed);
		}

		protected void FireOnException(Exception e)
		{
			OnException?.Invoke(this, e);
		}

		protected void FireCurrentProgress(double percent, string state)
		{
			CurrentDownloadProgress?.Invoke(this, new Tuple<double, string>(percent, state));
		}

		protected void FireTotalProgress(double percent, string state)
		{
			TotalDownloadProgress?.Invoke(this, new Tuple<double, string>(percent, state));
		}
	}
}
