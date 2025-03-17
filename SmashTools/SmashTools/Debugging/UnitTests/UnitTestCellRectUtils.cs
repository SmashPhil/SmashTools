using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using SmashTools.Performance;
using Verse;

namespace SmashTools.Debugging
{
  internal class UnitTestCellRectUtils : UnitTest
  {
    public override string Name => "CellRectUtils";

    public override TestType ExecuteOn => TestType.MainMenu;

    public override IEnumerable<UTResult> Execute()
    {
      yield return Cardinals();
      yield return AllCellsNoRepeat();
      yield return EdgeCellsSpan();
    }

    private UTResult Cardinals()
    {
      const int MaxSize = 5;

      UTResult result = new();

      IntVec3 position = new(10, 0, 10);

      IntVec3 north;
      IntVec3 east;
      IntVec3 south;
      IntVec3 west;

      CellRect cellRect = CellRect.SingleCell(position);
      List<IntVec3> cardinals = cellRect.Cardinals().ToList();
      result.Add($"Cardinals (1,1) Count", cardinals.Count == 1);
      result.Add($"Cardinals (1,1) Single", position == cardinals[0]);

      // 1x2 to 1xN
      for (int z = 2; z <= MaxSize; z++)
      {
        const int x = 1;
        cellRect = CellRect.CenteredOn(position, x, z);

        north = new(position.x, 0, cellRect.maxZ);
        south = new(position.x, 0, cellRect.minZ);
        cardinals = cellRect.Cardinals().ToList();
        result.Add($"Cardinals ({x},{z}) Count", cardinals.Count == 2);
        result.Add($"Cardinals ({x},{z}) North", north == cardinals[0]);
        result.Add($"Cardinals ({x},{z}) South", south == cardinals[1]);
      }

      // 2x1 to Nx1
      for (int x = 2; x <= MaxSize; x++)
      {
        const int z = 1;
        cellRect = CellRect.CenteredOn(position, x, z);

        east = new(cellRect.maxX, 0, position.z);
        west = new(cellRect.minX, 0, position.z);
        cardinals = cellRect.Cardinals().ToList();
        result.Add($"Cardinals ({x},{z}) Count", cardinals.Count == 2);
        result.Add($"Cardinals ({x},{z}) East", east == cardinals[0]);
        result.Add($"Cardinals ({x},{z}) West", west == cardinals[1]);
      }

      cellRect = CellRect.CenteredOn(position, 2, 2);

      IntVec3 northEast = new(cellRect.maxX, 0, cellRect.maxZ);
      IntVec3 southEast = new(cellRect.maxX, 0, cellRect.minZ);
      IntVec3 southWest = new(cellRect.minX, 0, cellRect.minZ);
      IntVec3 northWest = new(cellRect.minX, 0, cellRect.maxZ);

      // 2x2 special case
      cardinals = cellRect.Cardinals().ToList();
      result.Add($"Cardinals (2,2) Count", cardinals.Count == 4);
      result.Add($"Cardinals (2,2) North", northEast == cardinals[0]);
      result.Add($"Cardinals (2,2) South", southEast == cardinals[1]);
      result.Add($"Cardinals (2,2) East", southWest == cardinals[2]);
      result.Add($"Cardinals (2,2) West", northWest == cardinals[3]);

      // 2x2 to NxN
      for (int x = 2; x <= MaxSize; x++)
      {
        for (int z = 2; z <= MaxSize; z++)
        {
          // 2x2 is a special case, handled earlier. But we still need to test 2x3 and 3x2
          if (x == 2 && z == 2) continue;
          cellRect = CellRect.CenteredOn(position, x, z);

          north = new(position.x, 0, cellRect.maxZ);
          south = new(position.x, 0, cellRect.minZ);
          east = new(cellRect.maxX, 0, position.z);
          west = new(cellRect.minX, 0, position.z);

          cardinals = cellRect.Cardinals().ToList();
          result.Add($"Cardinals ({x},{z}) Count", cardinals.Count == 4);
          result.Add($"Cardinals ({x},{z}) North", north == cardinals[0]);
          result.Add($"Cardinals ({x},{z}) South", south == cardinals[1]);
          result.Add($"Cardinals ({x},{z}) East", east == cardinals[2]);
          result.Add($"Cardinals ({x},{z}) West", west == cardinals[3]);
        }
      }

      return result;
    }

    private UTResult AllCellsNoRepeat()
    {
      UTResult result = new();

      // t shape overlap
      Test(ref result, "t", new CellRect(0, 2, 8, 4), new CellRect(2, 0, 4, 8));
      // T shape overlap
      Test(ref result, "T_Left", new CellRect(0, 2, 8, 4), new CellRect(0, 0, 4, 8));
      Test(ref result, "T_Right", new CellRect(0, 2, 8, 4), new CellRect(4, 0, 4, 8));
      Test(ref result, "T_Top", new CellRect(0, 4, 8, 4), new CellRect(2, 0, 4, 8));
      Test(ref result, "T_Bottom", new CellRect(0, 0, 8, 4), new CellRect(2, 0, 4, 8));
      // L shape overlap
      Test(ref result, "L_BottomLeft", new CellRect(0, 0, 8, 4), new CellRect(0, 0, 4, 8));
      Test(ref result, "L_TopLeft", new CellRect(0, 4, 8, 4), new CellRect(0, 0, 4, 8));
      Test(ref result, "L_TopRight", new CellRect(0, 4, 8, 4), new CellRect(4, 0, 4, 8));
      Test(ref result, "L_BottomRight", new CellRect(0, 0, 8, 4), new CellRect(4, 0, 4, 8));
      // Partial overlap
      Test(ref result, "LeftHanging", new CellRect(4, 2, 8, 4), new CellRect(2, 0, 4, 8));
      Test(ref result, "RightHanging", new CellRect(0, 2, 8, 4), new CellRect(6, 0, 4, 8));
      Test(ref result, "TopHanging", new CellRect(0, 6, 8, 4), new CellRect(2, 0, 4, 8));
      Test(ref result, "BottomHanging", new CellRect(0, 0, 8, 4), new CellRect(2, 2, 4, 8));
      // Corner overlap
      Test(ref result, "BottomLeftCorner", new CellRect(0, 0, 8, 4), new CellRect(6, 2, 4, 8));
      Test(ref result, "TopLeftCorner", new CellRect(0, 6, 8, 4), new CellRect(6, 0, 4, 8));
      Test(ref result, "TopRightCorner", new CellRect(2, 6, 8, 4), new CellRect(0, 0, 4, 8));
      Test(ref result, "BottomRightCorner", new CellRect(2, 0, 8, 4), new CellRect(0, 2, 4, 8));
      // Full overlap
      Test(ref result, "Full", new CellRect(0, 2, 8, 4), new CellRect(0, 2, 8, 4));
      // No overlap
      Test(ref result, "None", new CellRect(0, 0, 8, 4), new CellRect(10, 10, 4, 8));

      return result;

      static void Test(ref UTResult result, string label, CellRect cellRect, CellRect otherRect)
      {
        var uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
        var extensionCells = cellRect.AllCellsNoRepeat(otherRect).ToList();

        // Distinct cells in result
        result.Add($"AllCellsNoRepeat_{label} (Distinct)",
          extensionCells.Count() == uniqueCells.Count());
        // Result is the same as if we enumerated both separately and filtered out duplicates
        result.Add($"AllCellsNoRepeat_{label} (Correct)", extensionCells.All(uniqueCells.Contains));

        // Ensure that order of rects does not matter
        Gen.Swap(ref cellRect, ref otherRect);

        uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
        extensionCells = cellRect.AllCellsNoRepeat(otherRect).ToList();

        // Distinct cells in result
        result.Add($"AllCellsNoRepeat_Swap_{label} (Distinct)",
          extensionCells.Count() == uniqueCells.Count());
        // Result is the same as if we enumerated both separately and filtered out duplicates
        result.Add($"AllCellsNoRepeat_Swap_{label} (Correct)",
          extensionCells.All(uniqueCells.Contains));
      }
    }

    private UTResult EdgeCellsSpan()
    {
      const int MapSize = 250;
      const int Padding = 5;

      UTResult result = new();

      CellRect mapRect = new(0, 0, MapSize, MapSize);

      // North
      CellRect cellRect = mapRect.EdgeCellsSpan(Rot4.North, size: Padding);
      CellRect resultRect = new(0, MapSize - Padding, MapSize, Padding);
      result.Add("EdgeCellsSpan (North)", cellRect == resultRect);
      result.Add("EdgeCellsSpan (CellCount)", resultRect.Cells.Count() == MapSize * Padding);

      // East
      cellRect = mapRect.EdgeCellsSpan(Rot4.East, size: Padding);
      resultRect = new(MapSize - Padding, 0, Padding, MapSize);
      result.Add("EdgeCellsSpan (North)", cellRect == resultRect);
      result.Add("EdgeCellsSpan (CellCount)", resultRect.Cells.Count() == MapSize * Padding);

      // South
      cellRect = mapRect.EdgeCellsSpan(Rot4.South, size: Padding);
      resultRect = new(0, 0, MapSize, Padding);
      result.Add("EdgeCellsSpan (North)", cellRect == resultRect);
      result.Add("EdgeCellsSpan (CellCount)", resultRect.Cells.Count() == MapSize * Padding);

      // West
      cellRect = mapRect.EdgeCellsSpan(Rot4.West, size: Padding);
      resultRect = new(0, 0, Padding, MapSize);
      result.Add("EdgeCellsSpan (North)", cellRect == resultRect);
      result.Add("EdgeCellsSpan (CellCount)", resultRect.Cells.Count() == MapSize * Padding);

      return result;
    }

    [DebugOutput(ProjectSetup.ProjectLabel, name = "Benchmark AllCellsNoRepeat")]
    private static unsafe void BenchmarkAllCellsNoRepeat()
    {
      const int SampleSize = 10000;
      IntVec3 position = new(3, 0, 3);

      CellRectContext test = new(position);

      Benchmark.Results hashsetResult = Benchmark.Run(SampleSize, &HashSetTest, ref test,
        measurement: Benchmark.Measurement.Milliseconds);
      Benchmark.Results extensionResult = Benchmark.Run(SampleSize, &ExtensionTest, ref test,
        measurement: Benchmark.Measurement.Milliseconds);

      Log.Message(
        $"HashSet ({hashsetResult.MeanString})\nExtension ({extensionResult.MeanString})");
    }

    private static void HashSetTest(ref CellRectContext context)
    {
      context.hashset.AddRange(context.normalRect.Cells);
      context.hashset.AddRange(context.rotatedRect.Cells);

      foreach (IntVec3 cell in context.hashset)
      {
      }

      context.hashset.Clear();
    }

    private static void ExtensionTest(ref CellRectContext context)
    {
      foreach (IntVec3 cell in context.normalRect.AllCellsNoRepeat(context.rotatedRect))
      {
      }
    }

    private struct CellRectContext
    {
      public readonly CellRect normalRect;
      public readonly CellRect rotatedRect;
      public HashSet<IntVec3> hashset;

      public CellRectContext(IntVec3 position)
      {
        normalRect = CellRect.CenteredOn(position, 5, 3);
        rotatedRect = CellRect.CenteredOn(position, 3, 5);
        hashset = [];
      }
    }
  }
}