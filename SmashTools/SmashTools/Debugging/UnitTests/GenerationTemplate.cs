using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;
using FieldInfo = System.Reflection.FieldInfo;

namespace SmashTools.Debugging;

public class GenerationTemplate
{
  public WorldT world = new();
  public MapT map = new();

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class WorldT
  {
    public float percent = 0.05f;

    public OverallRainfall rainfall = OverallRainfall.Normal;
    public OverallTemperature temperature = OverallTemperature.Normal;
    public OverallPopulation population = OverallPopulation.Normal;
  }

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class MapT
  {
    public BiomeDef biome;

    public IntVec2 size = new(150, 150);
    public HashSet<GenStepDef> disableGenSteps;

    // class Verse.MapGenerator
    private static Dictionary<string, object> data;
    private static FieldInfo playerStartSpotInt;

    static MapT()
    {
      // References to private fields
      data = (Dictionary<string, object>)AccessTools.Field(typeof(MapGenerator), "data")
       .GetValue(null);
      playerStartSpotInt = AccessTools.Field(typeof(MapGenerator), "playerStartSpotInt");

      Assert.IsNotNull(data);
      Assert.IsNotNull(playerStartSpotInt);
    }

    public Map GenerateMap(MapParent parent)
    {
      using MapState state = new();

      playerStartSpotInt.SetValue(null, IntVec3.Invalid);
      MapGenerator.rootsToUnfog.Clear();
      data.Clear();

      int seed = Gen.HashCombineInt(Find.World.info.Seed, parent.Tile);
      Rand.Seed = seed;

      if (parent.HasMap)
      {
        Assert.Fail($"Tried to generate a new map and set {parent} as its parent, but this world " +
          $"object already has a map. One world object can't have more than 1 map.");
        parent = null;
      }

      Map map = new();
      map.uniqueID = Find.UniqueIDsManager.GetNextMapID();
      map.generationTick = GenTicks.TicksGame;
      MapGenerator.mapBeingGenerated = map;
      map.info.Size = new IntVec3(size.x, 1, size.z);
      map.info.parent = parent;
      map.generatorDef = MapGeneratorDefOf.TestMapGenerator;
      map.ConstructComponents();
      Current.Game.AddMap(map);

      IEnumerable<GenStepWithParams> genSteps =
        MapGeneratorDefOf.TestMapGenerator.genSteps
         .Where(step => disableGenSteps.NullOrEmpty() || !disableGenSteps.Contains(step)).Select(
            step => new GenStepWithParams(step, default));

      map.areaManager.AddStartingAreas();
      map.weatherDecider.StartInitialWeather();
      MapGenerator.GenerateContentsIntoMap(genSteps, map, seed);
      Find.Scenario.PostMapGenerate(map);
      map.FinalizeInit();
      MapComponentUtility.MapGenerated(map);
      parent?.PostMapGenerate();

      return map;
    }

    private readonly struct MapState : IDisposable
    {
      private readonly ProgramState programState;

      public MapState()
      {
        programState = Current.ProgramState;
        Current.ProgramState = ProgramState.MapInitializing;
        MapGenerator.mapBeingGenerated = null;
        Rand.PushState();
      }

      void IDisposable.Dispose()
      {
        MapGenerator.mapBeingGenerated = null;
        Current.ProgramState = programState;
        Rand.PopState();
      }
    }
  }
}