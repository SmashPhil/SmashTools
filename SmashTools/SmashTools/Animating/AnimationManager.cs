using System;
using System.Collections.Generic;
using System.Linq;
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

		public AnimationManager(IAnimator animator, AnimationController controller)
		{
			this.controller = controller;
			layerDatas = new LayerData[controller.layers.Count];

			for (int i = 0; i < controller.layers.Count; i++)
			{
				layerDatas[i] = new LayerData(controller.layers[i]);
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
					layerData.frame = 0;
					StartTransition(layerData);
					continue;
				}
				//layerData.state.EvaluateFrame(layerData.frame);
				layerData.frame++;
			}
		}

		private void StartTransition(LayerData layerData)
		{
			// TODO - transitions need exitTime and blending implemented
			if (layerData.state.transitions.NullOrEmpty())
			{
				layerData.Reset();
			}
		}

		private class LayerData
		{
			public int frame; // Current frame in each layer
			public AnimationState state; // Active state in each layer
			public bool paused;
			public float originalValue;

			public readonly AnimationState defaultState;

			public LayerData(AnimationLayer layer)
			{
				defaultState = layer.states.FirstOrDefault(state => state.Type == StateType.Default);
				Reset();
			}

			public void Reset()
			{
				state = defaultState;
			}
		}
	}
}
