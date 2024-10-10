using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
	public class AnimationManager
	{
		public IAnimator animator;
		public readonly AnimationController controller;

		private readonly LayerData[] layerDatas;

		private Dictionary<int, AnimationParameter> parameters = new Dictionary<int, AnimationParameter>();

		public AnimationManager(IAnimator animator, AnimationController controller)
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
					parameters[parameter.Name.GetHashCode()] = parameter;
				}
			}
		}

		public void AnimationTick()
		{
			for (int i = 0; i < controller.layers.Count; i++)
			{
				LayerData layerData = layerDatas[i];
				// Check if transition is needed
				if (!layerData.state.clip || layerData.frame >= layerData.state.clip.frameCount)
				{
					Transition(layerData);
				}
				else
				{
					layerData.state.EvaluateFrame(animator, layerData.frame);
				}
				layerData.Update();
			}
		}

		private void Transition(LayerData layerData)
		{
			// TODO - transitions need exitTime and blending implemented
			if (layerData.state.transitions.NullOrEmpty())
			{
				layerData.Reset();
				return;
			}
			if (!layerData.Transitioning)
			{
				layerData.StartTransition();
			}
			if (layerData.TransitionTick >= layerData.transition.exitTicks)
			{
				layerData.StartNextState();
			}
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

		public void SetFloat(string name, float value)
		{
			SetFloat(name.GetHashCode(), value);
		}

		public void SetFloat(int id, float value)
		{
			if (parameters.TryGetValue(id, out AnimationParameter parameter))
			{
				FloatParam param = (FloatParam)parameter;
				param.value = value;
			}
		}

		public void SetInt(string name, int value)
		{
			SetInt(name.GetHashCode(), value);
		}

		public void SetInt(int id, int value)
		{
			if (parameters.TryGetValue(id, out AnimationParameter parameter))
			{
				IntParam param = (IntParam)parameter;
				param.value = value;
			}
		}

		public void SetBool(string name, bool value)
		{
			SetBool(name.GetHashCode(), value);
		}

		public void SetBool(int id, bool value)
		{
			if (parameters.TryGetValue(id, out AnimationParameter parameter))
			{
				BoolParam param = (BoolParam)parameter;
				param.value = value;
			}
		}

		public void SetTrigger(string name, bool value)
		{
			SetTrigger(name.GetHashCode(), value);
		}

		public void SetTrigger(int id, bool value)
		{
			if (parameters.TryGetValue(id, out AnimationParameter parameter))
			{
				TriggerParam param = (TriggerParam)parameter;
				param.value = value;
			}
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
				Reset();
			}

			public Dictionary<FieldInfo, float> Defaults { get; private set; } = new Dictionary<FieldInfo, float>();

			public int TransitionTick => frame - state.clip.frameCount;

			public bool Transitioning => transition != null;

			public bool WriteDefaults => state.writeDefaults && state.clip && !state.clip.properties.NullOrEmpty();

			public void Update()
			{
				frame++;
			}

			public void StartTransition()
			{
				foreach (AnimationTransition transition in state.transitions)
				{
					// TODO - incorporate condition check in transition rather than taking first case
					this.transition = transition;
					break;
				}
			}

			public void EvaluateTransition()
			{
			}

			public void SetState(AnimationState state)
			{
				RestoreDefaults();
				this.state = state;
				Reset();
				CacheDefaults();
			}

			public void StartNextState()
			{
				SetState(nextState);
			}

			private void CacheDefaults()
			{
				if (!WriteDefaults) return;

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
				frame = 0;
				nextState = null;
				state = defaultState;
			}
		}
	}
}
