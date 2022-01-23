using System.Collections.Generic;
using UnityEngine;

namespace SmashTools
{
	/// <summary>
	/// Render custom Inspect Pane for selected thing given interface method implementations
	/// </summary>
	public interface IInspectable
	{
		void DrawInspectDialog(Rect rect);

		float DoInspectPaneButtons(float x);
	}
}
