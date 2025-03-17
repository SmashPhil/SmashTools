using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace SmashTools;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class GenStep_RocksNearEdge : GenStep
{
  private List<Rot4> sides = [Rot4.Random];
  private int width = 20;

  private List<(RoofDef roofDef, float minGridValue)> rockThresholds =
  [
    (RoofDefOf.RoofRockThick, 1.14f),
    (RoofDefOf.RoofRockThin, 1.04f),
  ];

  public override int SeedPart => 1182952824;

  public override void Generate(Map map, GenStepParams parms)
  {
    const float MinElevation = 0.7f;

    if (map.TileInfo.WaterCovered) return;
    if (sides.NullOrEmpty()) return;

    map.regionAndRoomUpdater.Enabled = false;

    MapGenFloatGrid elevationGrid = MapGenerator.Elevation;
    MapGenFloatGrid cavesGrid = MapGenerator.Caves;
    CellRect mapRect = new(0, 0, map.Size.x, map.Size.z);
    foreach (Rot4 rot in sides)
    {
      foreach (IntVec3 intVec in mapRect.EdgeCellsSpan(rot, width))
      {
        float elevation = elevationGrid[intVec];
        if (elevation > MinElevation)
        {
          if (cavesGrid[intVec] <= 0f)
          {
            GenSpawn.Spawn(GenStep_RocksFromGrid.RockDefAt(intVec), intVec, map);
          }

          for (int i = 0; i < rockThresholds.Count; i++)
          {
            if (elevation > rockThresholds[i].minGridValue)
            {
              map.roofGrid.SetRoof(intVec, rockThresholds[i].roofDef);
              break;
            }
          }
        }
      }
    }

    BoolGrid visitedGrid = new BoolGrid(map);
    /*
  List<IntVec3> toRemove = new List<IntVec3>();
  Predicate<IntVec3> <> 9__0;
  Action<IntVec3> <> 9__1;
  foreach (IntVec3 intVec2 in map.AllCells)
  {
    if (!visited[intVec2] && this.IsNaturalRoofAt(intVec2, map))
    {
      toRemove.Clear();
      FloodFiller floodFiller = map.floodFiller;
      IntVec3 root = intVec2;
      Predicate<IntVec3> passCheck;
      if ((passCheck = <> 9__0) == null)
      {
        passCheck = (<> 9__0 = ((IntVec3 x) => this.IsNaturalRoofAt(x, map)));
      }
      Action<IntVec3> processor;
      if ((processor = <> 9__1) == null)
      {
        processor = (<> 9__1 = delegate (IntVec3 x)
        {
          visited[x] = true;
          toRemove.Add(x);
        });
      }
      floodFiller.FloodFill(root, passCheck, processor, int.MaxValue, false, null);
      if (toRemove.Count < 20)
      {
        for (int j = 0; j < toRemove.Count; j++)
        {
          map.roofGrid.SetRoof(toRemove[j], null);
        }
      }
    }
  }
  GenStep_ScatterLumpsMineable genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineable();
  genStep_ScatterLumpsMineable.maxValue = this.maxMineableValue;
  float num3 = 10f;
  switch (map.TileInfo.hilliness)
  {
    case Hilliness.Flat:
      num3 = 4f;
      break;
    case Hilliness.SmallHills:
      num3 = 8f;
      break;
    case Hilliness.LargeHills:
      num3 = 11f;
      break;
    case Hilliness.Mountainous:
      num3 = 15f;
      break;
    case Hilliness.Impassable:
      num3 = 16f;
      break;
  }
  if (this.overrideBlotchesPer10kCells != null)
  {
    num3 = this.overrideBlotchesPer10kCells.Value;
  }
  genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num3, num3);
  genStep_ScatterLumpsMineable.Generate(map, parms);
  map.regionAndRoomUpdater.Enabled = true;
    */
  }
}