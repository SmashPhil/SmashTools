using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
    public static class Ext_IDictionary
    {
        /// <summary>
        /// Grab random value from dictionary
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="dictionary"></param>
        public static KeyValuePair<T1, T2> RandomKVPFromDictionary<T1, T2>(this IDictionary<T1, T2> dictionary)
        {
            Rand.PushState();
            KeyValuePair<T1, T2> result = dictionary.ElementAt(Rand.Range(0, dictionary.Count));
            Rand.PopState();
            return result;
        }
    }
}
