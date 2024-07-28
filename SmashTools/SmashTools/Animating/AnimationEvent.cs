using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SmashTools.Animations
{
	public class AnimationEvent : IXmlExport
	{
		public int frame;
		public ResolvedMethod method;

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(frame), frame);
			XmlExporter.WriteObject(nameof(method), method);
		}
	}
}
