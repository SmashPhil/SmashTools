﻿using System;
using System.Linq;
using DevTools;
using UnityEngine;

namespace SmashTools
{
  public static class Ext_Rect
  {
    public static Rect[] SplitVertically(this Rect rect, int splits, float buffer = 0)
    {
      if (splits <= 1)
        throw new InvalidOperationException();

      float width = rect.width / splits - buffer * splits;
      Rect[] rects = new Rect[splits];
      for (int i = 0; i < splits; i++)
      {
        Rect splitRect = new Rect(rect.x + i * (width + buffer), rect.y, width, rect.height);
        rects[i] = splitRect;
      }

      return rects;
    }

    public static Rect[] SplitVertically(this Rect rect, float[] widthPercents,
      float buffer)
    {
      if (widthPercents.Length <= 1)
        throw new InvalidOperationException();

      int splits = widthPercents.Length;
      Assert.IsTrue(splits == widthPercents.Length, "Number of splits doesn't match widths array.");
      Assert.IsTrue(Mathf.Approximately(widthPercents.Sum(), 1),
        "Total width percentage doesn't equal 100%");

      float totalBuffer = (splits - 1) * buffer;
      float availableWidth = rect.width - totalBuffer;
      Rect[] rects = new Rect[splits];
      for (int i = 0; i < splits; i++)
      {
        float width = widthPercents[i] * availableWidth;
        Rect splitRect = new(rect.x + i * (width + buffer), rect.y, width, rect.height);
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