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
      yield return AllCellsNoRepeat();
    }

    public UTResult AllCellsNoRepeat()
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
        result.Add($"AllCellsNoRepeat_{label} (Distinct)", extensionCells.Count() == uniqueCells.Count());
        // Result is the same as if we enumerated both separately and filtered out duplicates
        result.Add($"AllCellsNoRepeat_{label} (Correct)", extensionCells.All(uniqueCells.Contains));

        // Ensure that order of rects does not matter
        Gen.Swap(ref cellRect, ref otherRect);

        uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
        extensionCells = cellRect.AllCellsNoRepeat(otherRect).ToList();

        // Distinct cells in result
        result.Add($"AllCellsNoRepeat_Swap_{label} (Distinct)", extensionCells.Count() == uniqueCells.Count());
        // Result is the same as if we enumerated both separately and filtered out duplicates
        result.Add($"AllCellsNoRepeat_Swap_{label} (Correct)", extensionCells.All(uniqueCells.Contains));
      }
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

      Log.Message($"HashSet ({hashsetResult.MeanString})\nExtension ({extensionResult.MeanString})");
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
