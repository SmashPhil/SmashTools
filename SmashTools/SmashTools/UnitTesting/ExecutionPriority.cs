using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.UnitTesting;

public enum ExecutionPriority
{
  Last = int.MinValue,
  BelowNormal = -100,
  Normal = 0,
  AboveNormal = 100,
  First = int.MaxValue
}