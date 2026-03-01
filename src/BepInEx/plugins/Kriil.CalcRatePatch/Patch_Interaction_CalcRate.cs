using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Kriil.Ostranauts.CalcRatePatch;

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

		// Target the Clamp call whose result is stored into fCTThemModifierUs.
		// This is more resilient than matching a hardcoded max such as 10f.
		for (int i = 0; i < codes.Count - 1; i++)
		{
			if (!Calls(codes[i], MI_MathfClamp) ||
				!IsFieldAccess(codes[i + 1], OpCodes.Stfld, FI_fCTThemModifierUs))
			{
				continue;
			}

			// Sanity check: this Clamp should be using the current fCTThemModifierUs
			// value as input somewhere shortly before the call.
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

			// Before the original Clamp call, the stack is:
			//   this, value, min, max
			// Add one more `this` for the hook:
			//   this, value, min, max, this
			codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
			codes[i + 1] = new CodeInstruction(OpCodes.Call, MI_ClampHook);
			patched = true;
			break;
		}

		if (!patched)
		{
			Debug.LogWarning("[kriil.ostranauts.calcratepatch] Failed to patch Interaction.CalcRate clamp block.");
		}

		return codes;
	}

	private static float ClampHook(float value, float min, float max, Interaction instance)
	{
		// Example policy:
		// keep the original min, allow raising the max cap, then use the real Clamp.
		if (instance != null && instance.strActionGroup == "Work")
		{
			max = Mathf.Max(max, 25f);
			value = 25f; // for testing, set to the new max to verify the hook is working.
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
