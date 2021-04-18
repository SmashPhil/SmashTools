using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public static class Ext_String
	{
		public static string ConvertRichText(this string text)
		{
			return SmashLog.ColorizeBrackets(text);
		}
	}
}
