using System.Collections.Generic;
using UnityEngine;

namespace SmashTools
{
	public interface IInspectable
	{
		void DrawInspectDialog(Rect rect);

		void DoInspectPaneButtons(float x);
	}
}
