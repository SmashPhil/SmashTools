﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmashTools.Xml;

namespace SmashTools.Animations
{
	public class AnimationTransition : IXmlExport, IDisposable
	{
		public int exitTicks;
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
			Trace.IsTrue(FromState.transitions.Remove(this));
			Trace.IsTrue(ToState.transitionsIncoming.Remove(this));

			FromState = null;
			ToState = null;
		}

		public AnimationTransition CreateCopy()
		{
			AnimationTransition copy = new AnimationTransition();
			copy.exitTicks = exitTicks;
			copy.conditions = new List<AnimationCondition>(conditions);

			return copy;
		}

		public void AddCondition()
		{
			AnimationCondition condition = new AnimationCondition();
			condition.Transition = this;
			condition.Parameter = FromState.Layer.Controller.parameters.FirstOrDefault();
			conditions.Add(condition);
		}

		internal void ResolveReferences()
		{
			if (!conditions.NullOrEmpty())
			{
				foreach (AnimationCondition condition in conditions)
				{
					condition.Transition = this;
					condition.ResolveReferences();
				}
			}
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(exitTicks), exitTicks);
			XmlExporter.WriteObject(nameof(toStateGuid), toStateGuid);
			XmlExporter.WriteCollection(nameof(conditions), conditions);
		}
	}
}
