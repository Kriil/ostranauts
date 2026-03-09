using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Interaction), "CalcRate")]
internal static class Patch_Interaction_CalcRate
{
	private static readonly FieldInfo FI_fCTThemModifierUs =
		AccessTools.Field(typeof(Interaction), "fCTThemModifierUs");

	private static readonly MethodInfo MI_MathfClamp =
		AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new[]
		{
			typeof(float),
			typeof(float),
			typeof(float)
		});

	private static readonly MethodInfo MI_ClampHook =
		AccessTools.Method(typeof(Patch_Interaction_CalcRate), nameof(ClampHook));

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
		bool patched = false;

		// Replace only the actor work-multiplier clamp writeback.
		// This preserves vanilla/FFU math and inserts the room bonus at the same point.
		for (int i = 0; i < codes.Count - 1; i++)
		{
			if (!Calls(codes[i], MI_MathfClamp) ||
				!IsFieldAccess(codes[i + 1], OpCodes.Stfld, FI_fCTThemModifierUs))
			{
				continue;
			}

			// Sanity check: the clamp should read fCTThemModifierUs shortly before call.
			bool foundSourceLoad = false;
			int searchStart = Mathf.Max(0, i - 8);
			for (int j = searchStart; j < i; j++)
			{
				if (IsFieldAccess(codes[j], OpCodes.Ldfld, FI_fCTThemModifierUs))
				{
					foundSourceLoad = true;
					break;
				}
			}

			if (!foundSourceLoad)
			{
				continue;
			}

			// Found the target clamp. Replace it with a call to our hook method.
			CodeInstruction originalCall = codes[i];

			// The hook needs the current Interaction instance for ship/context checks.
			// Move labels/exception blocks so control flow metadata stays attached.
			CodeInstruction loadInstance = new CodeInstruction(OpCodes.Ldarg_0)
			{
				labels = new List<Label>(originalCall.labels),
				blocks = new List<ExceptionBlock>(originalCall.blocks)
			};

			CodeInstruction replacementCall = new CodeInstruction(OpCodes.Call, MI_ClampHook);

			// Clear metadata on the removed instruction to avoid duplicate label ownership.
			originalCall.labels.Clear();
			originalCall.blocks.Clear();

			codes[i] = loadInstance;
			codes.Insert(i + 1, replacementCall);

			patched = true;
			break;
		}

		if (!patched)
		{
			UnityEngine.Debug.LogWarning("[kriil.ostranauts.roomeffects] Failed to patch Interaction.CalcRate clamp block.");
		}

		return codes;
	}

	public static float ClampHook(float value, float min, float max, Interaction instance)
	{
		if (instance?.objUs?.ship?.ShipCO != null)
		{
			// Use the player's current ship CondOwner as the ship-wide engineering source.
			float bonus = (float)instance.objUs.ship.ShipCO.GetCondAmount("StatShipEngineeringWorkBonus");
			if (bonus > 0f)
			{
				RoomEffectUtils.LogRoomEffect($"Applied engineering bonus of {bonus * 100f}% to '{instance.objUs.ship.ShipCO.strID}'.", "Engineering", null);
				value += bonus;
				// Add the bonus to the max as well to avoid capping the bonus.
				max += bonus;
			}
		}

		return Mathf.Clamp(value, min, max);
	}

	private static bool IsFieldAccess(CodeInstruction instruction, OpCode opCode, FieldInfo field)
	{
		return instruction.opcode == opCode && Equals(instruction.operand, field);
	}

	private static bool Calls(CodeInstruction instruction, MethodInfo method)
	{
		return (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
			Equals(instruction.operand, method);
	}
}
