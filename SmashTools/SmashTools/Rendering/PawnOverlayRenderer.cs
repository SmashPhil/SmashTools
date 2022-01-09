using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	public static class PawnOverlayRenderer
	{
		public static bool GetBodyPos(Pawn ___pawn, Vector3 drawLoc, out bool showBody, ref Vector3 __result)
		{
			showBody = true;
			if (___pawn.ParentHolder is IThingHolderPawnOverlayer)
			{
				__result = drawLoc;
				return false;
			}
			return true;
		}

		public static bool BodyAngle(ref float __result, Pawn ___pawn)
		{
			if (___pawn.ParentHolder is IThingHolderPawnOverlayer pawnOverlayer)
			{
				__result = pawnOverlayer.OverlayPawnBodyAngle;
				return false;
			}
			return true;
		}

		public static bool PawnOverlayerPosture(Pawn p, ref PawnPosture __result)
		{
			if (p.ParentHolder is IThingHolderPawnOverlayer)
			{
				__result = PawnPosture.LayingOnGroundNormal;
				return false;
			}
			return true;
		}

		public static bool LayingFacing(Pawn ___pawn, ref Rot4 __result)
		{
			if (___pawn.ParentHolder is IThingHolderPawnOverlayer pawnOverlayer)
			{
				__result = pawnOverlayer.PawnRotation;
				return false;
			}
			return true;
		}
	}
}
