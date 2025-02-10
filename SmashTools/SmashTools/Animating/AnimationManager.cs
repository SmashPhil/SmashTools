using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
	public class AnimationManager : IExposable
	{
		public IAnimator animator;
		public AnimationController controller;

		private LayerData[] layerDatas;

		private Dictionary<ushort, float> parameters = new Dictionary<ushort, float>();

		public AnimationManager(IAnimator animator, AnimationController controller)
		{
			Init(animator, controller);
			
		}

		public void Init(IAnimator animator, AnimationController controller)
		{
			this.animator = animator;
			this.controller = controller;

			layerDatas = new LayerData[controller.layers.Count];

			for (int i = 0; i < controller.layers.Count; i++)
			{
				layerDatas[i] = new LayerData(animator, controller.layers[i]);
			}
			if (!controller.parameters.NullOrEmpty())
			{
				foreach (AnimationParameter parameter in controller.parameters)
				{
					parameters[parameter.Id] = parameter.Value;
				}
			}
		}

		public void AnimationTick()
		{
			for (int i = 0; i < controller.layers.Count; i++)
			{
				LayerData layerData = layerDatas[i];
				if (!layerData.IsValid)
				{
					StartNextState(layerData);
					continue;
				}

				// Check if transition is needed
				if (layerData.frame >= layerData.state.clip.frameCount)
				{
					StartNextState(layerData);
				}
				else
				{
					layerData.state.EvaluateFrame(animator, layerData.frame);
				}
				layerData.Update();
			}
		}

		void IExposable.ExposeData()
		{
			Scribe_Collections.Look(ref parameters, nameof(parameters), keyLookMode: LookMode.Value, valueLookMode: LookMode.Value);
		}

		// TODO - transitions need exitTime and blending implemented
		private void Transition(LayerData layerData)
		{
			if (!layerData.Transitioning)
			{
				layerData.EvaluateTransition();
			}
			if (layerData.TransitionTick >= layerData.transition.exitTicks)
			{
				StartNextState(layerData);
			}
		}

		private void StartNextState(LayerData layerData)
		{
			Assert.IsNotNull(layerData.state.transitions);

			foreach (AnimationTransition transition in layerData.state.transitions)
			{
				if (transition.conditions.NullOrEmpty())
				{
					layerData.SetState(transition.ToState);
					return;
				}

				foreach (AnimationCondition condition in transition.conditions)
				{
					float value = parameters[condition.def.shortHash];
					if (condition.ConditionMet(value))
					{
						layerData.SetState(transition.ToState);
						return;
					}
				}
			}
			if (layerData.IsValid && layerData.state.clip.loop)
			{
				layerData.frame = 0;
			}
			// End of animation chain, state will wait for transition till one becomes available
		}

		internal (AnimationState state, int frame) CurrentFrame(AnimationLayer layer)
		{
			foreach (LayerData layerData in layerDatas)
			{
				if (layerData.layer == layer)
				{
					return (layerData.state, layerData.frame);
				}
			}
			return (null, 0);
		}

		internal void SetFrame(AnimationClip clip, int frame)
		{
			AnimationState.EvaluateFrame(clip, animator, frame);
		}

		public void SetFloat(string name, float value)
		{
			SetFloat(DefDatabase<AnimationParameterDef>.GetNamed(name), value);
		}

		public void SetFloat(AnimationParameterDef paramDef, float value)
		{
			Assert.IsNotNull(paramDef);
			SetFloat(paramDef.shortHash, value);
		}

		public void SetFloat(ushort id, float value)
		{
			// All ids should be precached but this isn't error-causing so this
			// is strictly just for notifying the operation is useless.
			Assert.IsTrue(parameters.ContainsKey(id), "Parameter Id not precached");
			parameters[id] = value;
		}

		public void SetInt(string name, int value)
		{
			SetInt(DefDatabase<AnimationParameterDef>.GetNamed(name), value);
		}

		public void SetInt(AnimationParameterDef paramDef, int value)
		{
			SetInt(paramDef.shortHash, value);
		}

		public void SetInt(ushort id, int value)
		{
			SetFloat(id, value);
		}

		public void SetBool(string name, bool value)
		{
			SetBool(DefDatabase<AnimationParameterDef>.GetNamed(name), value);
		}

		public void SetBool(AnimationParameterDef paramDef, bool value)
		{
			SetBool(paramDef.shortHash, value);
		}

		public void SetBool(ushort id, bool value)
		{
			SetFloat(id, value ? 1 : 0);
		}

		public void SetTrigger(string name, bool value)
		{
			SetTrigger(DefDatabase<AnimationParameterDef>.GetNamed(name), value);
		}

		public void SetTrigger(AnimationParameterDef paramDef, bool value)
		{
			SetTrigger(paramDef.shortHash, value);
		}

		public void SetTrigger(ushort id, bool value)
		{
			SetFloat(id, value ? 1 : 0);
		}

		private class LayerData
		{
			public readonly IAnimator animator;
			public readonly AnimationLayer layer;
			public readonly AnimationState defaultState;
			
			public int frame; // Current frame in each layer
			public AnimationState state; // Active state in each layer
			public AnimationState nextState;
			public AnimationTransition transition;
			public bool paused;

			public LayerData(IAnimator animator, AnimationLayer layer)
			{
				this.animator = animator;
				this.layer = layer;
				defaultState = layer.states.FirstOrDefault(state => state.Type == StateType.Default);
				state = defaultState;
			}

			public Dictionary<FieldInfo, float> Defaults { get; private set; } = new Dictionary<FieldInfo, float>();

			// Invalid states are treated as empty. Will immediately transition to the next state
			public bool IsValid => state.clip;

			public int TransitionTick => frame - state.clip.frameCount;

			public bool Transitioning => transition != null;

			public bool WriteDefaults => state.writeDefaults && state.clip && !state.clip.properties.NullOrEmpty();

			public void Update()
			{
				frame++;
			}

			public void EvaluateTransition()
			{
				throw new NotImplementedException();
			}

			public void SetState(AnimationState state)
			{
				RestoreDefaults();
				frame = 0;
				if (state.Type == StateType.Exit)
				{
					state = defaultState;
				}
				this.state = state;
				CacheDefaults();
			}

			private void CacheDefaults()
			{
				if (!WriteDefaults || !IsValid) return;

				Defaults.Clear();
				foreach (AnimationPropertyParent propertyParent in state.clip.properties)
				{
					foreach (AnimationProperty property in propertyParent)
					{
						float startingValue = property.GetProperty(animator);
						Defaults[property.FieldInfo] = startingValue;
					}
				}
			}

			private void RestoreDefaults()
			{
				if (!WriteDefaults) return;

				foreach (AnimationPropertyParent propertyParent in state.clip.properties)
				{
					foreach (AnimationProperty property in propertyParent)
					{
						float value = Defaults[property.FieldInfo];
						property.SetProperty(animator, value);
					}
				}
			}

			public void Reset()
			{
				SetState(defaultState);
			}
		}
	}
}
