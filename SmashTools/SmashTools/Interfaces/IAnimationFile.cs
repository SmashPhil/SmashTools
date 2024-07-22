using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public interface IAnimationFile : IXmlExport
	{
		public string FilePath { get; set; }

		public string FileName { get; set; }
	}
}
