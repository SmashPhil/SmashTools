using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmashTools
{
	public static class Ext_Rect
	{
		public static Rect[] Split(this Rect rect, int splits, float buffer = 0)
		{
			if (splits < 0)
			{
				throw new InvalidOperationException();
			}
			if (splits == 1)
			{
				return new Rect[] { rect };
			}
			float width = rect.width / splits - buffer;
			Rect[] rects = new Rect[splits];
			for (int i = 0; i < splits; i++)
			{
				Rect splitRect = new Rect(i * width + buffer, rect.y, width, rect.height);
				rects[i] = splitRect;
			}
			return rects;
		}
	}
}
