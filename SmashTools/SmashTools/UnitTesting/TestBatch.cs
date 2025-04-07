using System.Collections.Generic;
using System.Linq;

namespace SmashTools.UnitTesting;

internal class TestBatch
{
  public readonly UnitTest unitTest;
  public readonly List<UTResult> results = [];

  public string message;

  public TestBatch(UnitTest unitTest)
  {
    this.unitTest = unitTest;
  }

  public int Count => results.Count;

  public bool Failed { get; private set; }

  public bool ShowMessage { get; private set; }

  public void FailWithMessage(string reason)
  {
    message = reason.ConvertRichText();
    Failed = true;
    ShowMessage = true;
  }

  public void Add(UTResult result)
  {
    results.Add(result);
    Failed = result.Tests.Any(r => r.result == UTResult.Result.Failed);
  }
}