using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.UnitTesting;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class TestBlock
{
  public TestType type;
  public List<Type> tests;
  public GenerationTemplate template;

  public List<UnitTest> UnitTests { get; } = [];

  public void CreateTests()
  {
    if (tests.NullOrEmpty()) return;

    UnitTests.Clear();
    foreach (Type testType in tests)
    {
      UnitTests.Add((UnitTest)Activator.CreateInstance(testType));
    }
  }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class TestSuiteDef : Def
{
  public List<TestBlock> plan;

  public override void PostLoad()
  {
    if (!plan.NullOrEmpty())
    {
      foreach (TestBlock block in plan)
      {
        block.CreateTests();
      }
    }
  }

  public override IEnumerable<string> ConfigErrors()
  {
    foreach (string error in base.ConfigErrors())
    {
      yield return error;
    }

    if (plan.NullOrEmpty())
    {
      yield return "Empty test plan";
    }
    else
    {
      TestType type = TestType.Disabled;
      foreach (TestBlock block in plan)
      {
        if (block.type == type)
        {
          yield return "Redundant TestBlock types";
        }

        type = block.type;
        if (type == TestType.Disabled)
        {
          yield return "Disabled test in test plan.";
        }

        if (block.template != null && block.type != TestType.Playing)
        {
          yield return "MapTemplate defined with TestType that is not set to Playing.";
        }
      }
    }
  }
}