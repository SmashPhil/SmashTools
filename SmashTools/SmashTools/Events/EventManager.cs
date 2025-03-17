using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
  public class EventManager<T>
  {
    public readonly Dictionary<T, EventTrigger> lookup = [];

    public bool Enabled { get; set; } = true;

    public EventTrigger this[T key]
    {
      get
      {
        if (!lookup.ContainsKey(key))
        {
          lookup[key] = new EventTrigger();
        }

        return lookup[key];
      }
      set { lookup[key] = value; }
    }
  }
}