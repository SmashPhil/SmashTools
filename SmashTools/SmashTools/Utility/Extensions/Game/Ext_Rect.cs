using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public static class Ext_Rect
	{
		public static IEnumerable<IntVec3> CellsNoOverlap(this CellRect cellRect, CellRect excludeRect)
		{
			HashSet<IntVec3> noOverlapCells = excludeRect.Cells.ToHashSet();
			foreach (IntVec3 cell in cellRect)
			{ 
				if (!noOverlapCells.Contains(cell))
				{
					yield return cell;
				}
			}
		}

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
			float width = rect.width / splits - buffer * splits;
			Rect[] rects = new Rect[splits];
			for (int i = 0; i < splits; i++)
			{
				Rect splitRect = new Rect(rect.x + i * (width + buffer), rect.y, width, rect.height);
				rects[i] = splitRect;
			}
			return rects;
		}
	}
}
