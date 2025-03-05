using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
  public static class Ext_Rect
  {
    public static IEnumerable<IntVec3> Cardinals(this CellRect cellRect)
    {
      if (cellRect.IsEmpty) yield break;
      if (cellRect.Area == 1) yield return new IntVec3(cellRect.minX, 0, cellRect.minZ);

      if (cellRect.Width > 1)
      {
        yield return new IntVec3(cellRect.minX, 0, 0);
        yield return new IntVec3(cellRect.maxX, 0, 0);
      }
      if (cellRect.Height > 1)
      {
        yield return new IntVec3(0, 0, cellRect.minZ);
        yield return new IntVec3(0, 0, cellRect.maxZ);
      }
    }

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

    /// <summary>
    /// Enumerate over all cells in <paramref name="cellRect"/> and <paramref name="otherRect"/> without 
    /// repeating cells from overlapping sections.
    /// </summary>
    public static IEnumerable<IntVec3> AllCellsNoRepeat(this CellRect cellRect, CellRect otherRect)
    {
      // Same rects, returning the cells of 1 is fine
      if (cellRect == otherRect)
      {
        foreach (IntVec3 cell in cellRect) yield return cell;
        yield break;
      }
      // No overlap, will be faster to return both separately
      if (!cellRect.Overlaps(otherRect))
      {
        foreach (IntVec3 cell in cellRect) yield return cell;
        foreach (IntVec3 cell in otherRect) yield return cell;
        yield break;
      }

      // CellRect will have 8 boundary lines, we need to separate out which rects extend beyond
      // the overlapping section.
      // NOTE - CellRect subtracts 1 from max limits since max is the indicator of max cell value,
      // not max boundary line which would include the last cell unit. We need to adjust here and
      // decrement 1 when starting at max bounds for cell indices.

      int maxTopZ, maxBotZ;     // Top Bounds
      int minTopZ, minBotZ;     // Bottom Bounds
      int minLeftX, minRightX;  // Left Bounds
      int maxLeftX, maxRightX;  // Right Bounds

      // Needs expanded iteration to catch floating corners when rect is not in a cross pattern
      // and only partially overlaps.
      RectEdge edge = RectEdge.None;

      // Top
      if (cellRect.maxZ > otherRect.maxZ)
      {
        maxTopZ = cellRect.maxZ + 1;
        maxBotZ = otherRect.maxZ + 1;
        if (cellRect.Width > otherRect.Width) edge |= RectEdge.Top;
      }
      else
      {
        maxTopZ = otherRect.maxZ + 1;
        maxBotZ = cellRect.maxZ + 1;
        if (otherRect.Width > cellRect.Width) edge |= RectEdge.Top;
      }

      // Bottom
      if (cellRect.minZ > otherRect.minZ)
      {
        minTopZ = cellRect.minZ;
        minBotZ = otherRect.minZ;
        if (cellRect.Width < otherRect.Width) edge |= RectEdge.Bottom;
      }
      else
      {
        minTopZ = otherRect.minZ;
        minBotZ = cellRect.minZ;
        if (otherRect.Width < cellRect.Width) edge |= RectEdge.Bottom;
      }

      // Left
      if (cellRect.minX < otherRect.minX)
      {
        minLeftX = cellRect.minX;
        minRightX = otherRect.minX;
        if (cellRect.Height > otherRect.Height) edge |= RectEdge.Left;
      }
      else
      {
        minLeftX = otherRect.minX;
        minRightX = cellRect.minX;
        if (otherRect.Height > cellRect.Height) edge |= RectEdge.Left;
      }

      // Right
      if (cellRect.maxX < otherRect.maxX)
      {
        maxLeftX = cellRect.maxX + 1;
        maxRightX = otherRect.maxX + 1;
        if (cellRect.Height < otherRect.Height) edge |= RectEdge.Right;
      }
      else
      {
        maxLeftX = otherRect.maxX + 1;
        maxRightX = cellRect.maxX + 1;
        if (otherRect.Height < cellRect.Height) edge |= RectEdge.Right;
      }

      int x, z, xLimit, zLimit;

      // Left no-overlap
      x = minLeftX;
      xLimit = minRightX;
      for (; x < xLimit; x++)
      {
        z = edge == RectEdge.Left ? maxTopZ - 1 : maxBotZ - 1;
        zLimit = edge == RectEdge.Left ? minBotZ : minTopZ;
        for (; z >= zLimit; z--)
        {
          yield return new IntVec3(x, 0, z);
        }
      }

      // Right no-overlap
      x = maxLeftX;
      xLimit = maxRightX;
      for (; x < xLimit; x++)
      {
        z = edge == RectEdge.Right ? maxTopZ - 1 : maxBotZ - 1;
        zLimit = edge == RectEdge.Right ? minBotZ : minTopZ;
        for (; z >= zLimit; z--)
        {
          yield return new IntVec3(x, 0, z);
        }
      }

      // Top no-overlap
      x = edge == RectEdge.Top ? minLeftX : minRightX;
      xLimit = edge.HasFlag(RectEdge.Top) ? maxRightX : maxLeftX;
      switch (edge)
      {
        // Corners mirror limits
        case RectEdge.TopLeft or RectEdge.BottomRight:
          x = minLeftX;
          xLimit = maxLeftX;
          break;
        case RectEdge.TopRight or RectEdge.BottomLeft:
          x = minRightX;
          xLimit = maxRightX;
          break;
      }
      for (; x < xLimit; x++)
      {
        z = maxTopZ - 1;
        zLimit = maxBotZ;
        for (; z >= zLimit; z--)
        {
          yield return new IntVec3(x, 0, z);
        }
      }

      // Bottom no-overlap
      x = edge == RectEdge.Bottom ? minLeftX : minRightX;
      xLimit = edge.HasFlag(RectEdge.Bottom) ? maxRightX : maxLeftX;
      switch (edge)
      {
        // Corners mirror limits
        case RectEdge.TopLeft or RectEdge.BottomRight:
          x = minRightX;
          xLimit = maxRightX;
          break;
        case RectEdge.TopRight or RectEdge.BottomLeft:
          x = minLeftX;
          xLimit = maxLeftX;
          break;
      }
      for (; x < xLimit; x++)
      {
        z = minTopZ - 1;
        zLimit = minBotZ;
        for (; z >= zLimit; z--)
        {
          yield return new IntVec3(x, 0, z);
        }
      }

      // Overlapping section
      for (x = minRightX; x < maxLeftX; x++)
      {
        for (z = maxBotZ - 1; z >= minTopZ; z--)
        {
          yield return new IntVec3(x, 0, z);
        }
      }
      yield break;
    }

    public static Rect[] SplitVertically(this Rect rect, int splits, float buffer = 0)
    {
      if (splits < 0)
      {
        throw new InvalidOperationException();
      }
      if (splits == 1)
      {
        return [rect];
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

    public static Rect[] SplitVertically(this Rect rect, int splits, float[] widthPercents, float buffer = 0)
    {
      if (splits <= 0)
      {
        throw new InvalidOperationException();
      }
      if (splits == 1)
      {
        return new Rect[] { rect };
      }
      Assert.IsTrue(splits == widthPercents.Length, "Number of splits doesn't match widths array.");
      Assert.IsTrue(Mathf.Approximately(widthPercents.Sum(), 1), "Total width percentage doesn't equal 100%");

      float totalBuffer = (splits - 1) * buffer;
      float availableWidth = rect.width - totalBuffer;
      Rect[] rects = new Rect[splits];
      for (int i = 0; i < splits; i++)
      {
        float width = widthPercents[i] * availableWidth;
        Rect splitRect = new Rect(rect.x + i * (width + buffer), rect.y, width, rect.height);
        rects[i] = splitRect;
      }
      return rects;
    }

    [Flags]
    private enum RectEdge
    {
      None,
      Left = 1 << 0,
      Right = 1 << 1,
      Top = 1 << 2,
      Bottom = 1 << 3,
      // NOTE - Edges are parts of the rect that are hanging off. If 2 edges are
      // hanging off then the corner will be opposite to the cardinal edge that
      // is hanging off eg. Top edge + right hanging = top left corner is overlapping.
      BottomLeft = Bottom | Right,
      TopLeft = Top | Right,
      TopRight = Top | Left,
      BottomRight = Bottom | Left,
    }
  }
}
