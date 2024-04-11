using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public interface IThingHolderPawnOverlayer : IThingHolderWithDrawnPawn
	{
		public Rot4 PawnRotation { get; }

		public bool ShowBody { get; }
	}
}
