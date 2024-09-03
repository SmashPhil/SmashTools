using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmashTools.Xml;

namespace SmashTools.Animations
{
	public abstract class AnimationParameter : IXmlExport
	{
		public string name;
		
		public abstract string ParamName { get; }

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(name), name);
		}
	}

	public class BoolParam : AnimationParameter
	{
		public bool value;

		public override string ParamName => "Bool";
	}

	public class TriggerParam : BoolParam
	{
		public override string ParamName => "Trigger";
	}

	public class IntParam : AnimationParameter
	{
		public int value;

		public override string ParamName => "Int";
	}

	public class FloatParam : AnimationParameter
	{
		public float value;

		public override string ParamName => "Float";
	}
}
