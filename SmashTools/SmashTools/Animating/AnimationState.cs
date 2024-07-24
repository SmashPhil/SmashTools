using SmashTools.Xml;
using System.Collections.Generic;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationState : IXmlExport
	{
		public string name;
		public IntVec2 position;
		public AnimationClip clip;
		public float speed = 1;
		public bool writeDefaults = true;

		private StateType stateType = StateType.None;

		public List<AnimationTransition> transitions;
		
		/// <summary>
		/// For XML Deserialization
		/// </summary>
		public AnimationState()
		{
		}

		public AnimationState(string name)
		{
			this.name = name;
		}

		public AnimationState(string name, StateType stateType)
		{
			this.name = name;
			this.stateType = stateType;
		}

		public StateType Type => stateType;

		public void AddTransition(AnimationState to)
		{
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(name), name);
			XmlExporter.WriteObject(nameof(position), position);
			XmlExporter.WriteObject(nameof(clip), clip.FilePath);
			XmlExporter.WriteObject(nameof(speed), speed);
			XmlExporter.WriteObject(nameof(writeDefaults), writeDefaults);

			XmlExporter.WriteObject(nameof(stateType), stateType);

			XmlExporter.WriteList(nameof(transitions), transitions);
		}

		public enum StateType
		{
			None,
			Entry,
			Default,
			Exit,
			AnyState
		}
	}
}
