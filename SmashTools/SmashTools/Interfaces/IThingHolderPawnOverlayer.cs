using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SmashTools
{
	//[MustImplement("DrawPawnOverlay")]
	public interface IThingHolderPawnOverlayer : IThingHolder
	{
		public Rot4 PawnRotation { get; }

		public float OverlayPawnBodyAngle { get; }
	}
}
