using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	public static class PawnOverlayRenderer
	{
		public static IEnumerable<CodeInstruction> ShowBodyTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			MethodInfo heldPawnPropertyGetter = AccessTools.PropertyGetter(typeof(IThingHolderWithDrawnPawn), nameof(IThingHolderWithDrawnPawn.HeldPawnDrawPos_Y));
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(heldPawnPropertyGetter))
				{
					yield return instruction; //Stfld : Vector3.y from get_HeldPawnDrawPos_Y
					instruction = instructionList[++i];

					yield return new CodeInstruction(opcode: OpCodes.Ldarg_2); //showBody for address assignment
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 7); //IThingHolderWithDrawnPawn instance
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_2); //showBody arg
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(PawnOverlayRenderer), nameof(GetShowBody)));
					yield return new CodeInstruction(opcode: OpCodes.Stind_I1); //stores result at showBody's address
				}

				yield return instruction;
			}
		}

		public static bool GetShowBody(IThingHolderWithDrawnPawn thingHolderWithDrawnPawn, bool showBody)
		{
			if (thingHolderWithDrawnPawn is IThingHolderPawnOverlayer pawnOverlayer)
			{
				showBody = pawnOverlayer.ShowBody;
			}
			return showBody;
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
