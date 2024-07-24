using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
	public class AnimationLayer : IXmlExport
	{
		public string name;
		
		public List<AnimationState> states = new List<AnimationState>();

		/// <summary>
		/// For Xml Deserialization
		/// </summary>
		private AnimationLayer()
		{
		}

		public AnimationLayer(string name)
		{
			this.name = name;
		}

		public AnimationState AddState(string name, IntVec2 position, StateType type = StateType.None)
		{
			if (type != StateType.None && states.Any(state => state.Type == type))
			{
				Log.Error($"Attempting to load duplicate special state type to AnimationLayer {name}.");
				return null;
			}
			if (type == StateType.None && !states.Any(state => state.Type == StateType.Default))
			{
				type = StateType.Default;
			}
			AnimationState state = new AnimationState(name, type);
			state.position = position;
			if (type == StateType.Default)
			{
				//TODO - Add Transition from entry to default that cannot be removed
			}
			states.Add(state);
			return state;
		}

		public void RemoveState(string name)
		{
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].name == name)
				{
					states.RemoveAt(i);
					return;
				}
			}
		}

		public static AnimationLayer CreateLayer(string name)
		{
			AnimationLayer layer = new AnimationLayer(name);
			layer.AddState("Entry", new IntVec2(-10, 0), type: StateType.Entry);
			//layer.AddState("Any State", new IntVec2(-10, 5), type: StateType.AnyState);
			layer.AddState("Exit", new IntVec2(10, 0), type: StateType.Exit);

			return layer;
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(name), name);
			XmlExporter.WriteList(nameof(states), states);
		}
	}
}
