using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
  public interface IPoolable
  {
    bool InPool { get; set; }

    /// <summary>
    /// Clear all references as object is being returned to pool
    /// </summary>
    public void Reset();
  }
}
