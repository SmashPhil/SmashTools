using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationState : IXmlExport, IDisposable
	{
		public string name;
		public Guid guid;
		public IntVec2 position;
		public AnimationClip clip;
		public float speed = 1;
		public bool writeDefaults = true;

		private StateType stateType = StateType.None;

		public List<AnimationTransition> transitions = new List<AnimationTransition>();
		
		/// <summary>
		/// For XML Deserialization
		/// </summary>
		public AnimationState()
		{
		}

		public AnimationState(string name, StateType stateType)
		{
			this.name = name;
			this.stateType = stateType;
			guid = Guid.NewGuid();
		}

		public StateType Type => stateType;

		public void AddTransition(AnimationState to)
		{
			AnimationTransition transition = new AnimationTransition(this, to);
			transitions.Add(transition);
		}

		public void Dispose()
		{
			for (int i = transitions.Count - 1; i >= 0; i--)
			{
				transitions[i].Dispose();
			}
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(name), name);
			XmlExporter.WriteObject(nameof(guid), guid);
			XmlExporter.WriteObject(nameof(position), position);
			XmlExporter.WriteObject(nameof(clip), clip?.FilePath);
			XmlExporter.WriteObject(nameof(speed), speed);
			XmlExporter.WriteObject(nameof(writeDefaults), writeDefaults);

			XmlExporter.WriteObject(nameof(stateType), stateType);

			XmlExporter.WriteList(nameof(transitions), transitions);
		}

		// Pass in layer to add to, allowing for copy / pasting across layers
		public AnimationState CreateCopy(AnimationLayer layer)
		{
			AnimationState copy = new AnimationState();
			copy.name = AnimationLoader.GetAvailableName(layer.states.Select(l => l.name), name);
			copy.guid = Guid.NewGuid();
			copy.position = position;
			copy.clip = clip;
			copy.speed = speed;
			copy.writeDefaults = writeDefaults;
			copy.stateType = stateType;
			copy.transitions = transitions.Select(transition => transition.CreateCopy()).ToList();

			return copy;
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
