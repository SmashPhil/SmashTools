using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmashTools.Xml;

namespace SmashTools.Animations
{
	public class AnimationTransition : IXmlExport, IDisposable
	{
		public float exitTime;
		public Guid toStateGuid; // guid of ToState for lookup
		public List<AnimationCondition> conditions = new List<AnimationCondition>();

		public AnimationTransition()
		{
		}

		public AnimationTransition(AnimationState from, AnimationState to)
		{
			FromState = from;
			ToState = to;
			toStateGuid = to.guid;
		}

		public AnimationState FromState { get; internal set; }

		public AnimationState ToState { get; internal set; }

		public bool DefaultTransition => FromState != null && FromState.Type == AnimationState.StateType.Entry &&
										 ToState != null && ToState.Type == AnimationState.StateType.Default;

		public void Dispose()
		{
			FromState.transitions.Remove(this);
			ToState.transitions.Remove(this);

			FromState = null;
			ToState = null;
		}

		public AnimationTransition CreateCopy()
		{
			AnimationTransition copy = new AnimationTransition();
			copy.exitTime = exitTime;
			copy.conditions = new List<AnimationCondition>(conditions);

			return copy;
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(exitTime), exitTime);
			XmlExporter.WriteObject(nameof(toStateGuid), toStateGuid);
			XmlExporter.WriteCollection(nameof(conditions), conditions);
		}

		//needs conditions

		//needs exit time
	}
}
