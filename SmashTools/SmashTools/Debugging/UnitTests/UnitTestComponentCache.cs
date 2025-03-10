using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using SmashTools.Performance;
using Verse;

namespace SmashTools.Debugging;

internal class UnitTestComponentCache : UnitTest
{
  public override string Name => "ComponentCache";

  public override TestType ExecuteOn => TestType.MainMenu;

  public override IEnumerable<UTResult> Execute()
  {
    UTResult result = new();
    Assert.IsTrue(Current.ProgramState == ProgramState.Entry);

    result.Add("ComponentCache (GameComps)", ComponentCache.gameComps.Count == 0);
    result.Add("ComponentCache (WorldComps)", ComponentCache.worldComps.Count == 0);
    result.Add("ComponentCache (MapComps)", MapComponentCache.CountAll() == 0);

    yield return result;
  }
}