using System.Collections.Generic;
using DevTools;
using DevTools.UnitTesting;
using Verse;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
internal class UnitTest_ComponentCache
{
  [Test]
  private void Clear()
  {
    Assert.IsTrue(Current.ProgramState == ProgramState.Entry);

    Expect.IsTrue("GameComps", ComponentCache.gameComps.Count == 0);
    Expect.IsTrue("WorldComps", ComponentCache.worldComps.Count == 0);
    Expect.IsTrue("MapComps", MapComponentCache.CountAll() == 0);
  }
}