using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SmashTools.Rendering;

// TODO - work in progress, LineStrip width needs to be configurable
[PublicAPI]
internal static class Ext_Mesh
{
  [MustUseReturnValue]
  public static Mesh BakeLineRendererMesh(List<LineSegment> segments)
  {
    if (segments == null)
      throw new ArgumentNullException(nameof(segments), "Line segments cannot be null.");
    if (segments.Count == 0)
      throw new ArgumentException("Line segments cannot be empty.", nameof(segments));

    Mesh lineMesh = new()
    {
      name = "Line segments mesh"
    };

    try
    {
      RecalculateLineRenderer(lineMesh, segments);
      return lineMesh;
    }
    catch
    {
      Object.Destroy(lineMesh);
      throw;
    }
  }

  public static void RecalculateLineRenderer([NotNull] Mesh mesh, List<LineSegment> segments)
  {
    if (!mesh)
      throw new ArgumentNullException(nameof(mesh), "Mesh cannot be null.");
    if (segments == null)
      throw new ArgumentNullException(nameof(segments), "Line segments cannot be null.");
    if (segments.Count == 0)
      throw new ArgumentException("Line segments cannot be empty.", nameof(segments));

    int segmentCount = segments.Count;
    int totalVertices = segmentCount + 1;

    // UInt32 might not be supported on some platforms but for desktop this should be fine as a fallback.
    if (totalVertices >= ushort.MaxValue)
      mesh.indexFormat = IndexFormat.UInt32;

    Vector3[] vertices = new Vector3[totalVertices];
    int[] indices = new int[totalVertices];
    Color[] colors = new Color[totalVertices];

    vertices[0] = segments[0].from;
    colors[0] = segments[0].color;
    indices[0] = 0;
    for (int i = 0; i < segmentCount; i++)
    {
      LineSegment segment = segments[i];
      int idx = i + 1;
      vertices[idx] = segment.to;
      colors[idx] = segment.color;
      indices[idx] = idx;
    }
    mesh.Clear();
    mesh.SetVertices(vertices);
    mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
    mesh.SetColors(colors);
    mesh.RecalculateBounds();
  }
}