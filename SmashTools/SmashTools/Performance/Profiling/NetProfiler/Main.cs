using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Performance
{
	public class Main
	{
		[UnmanagedCallersOnly]
		public static unsafe int DllGetClassObject(Guid* rclsid, Guid* riid, IntPtr* ppv)
		{
			return 0;
		}
	}
}
