using HarmonyLib;
using UnityEngine;

namespace Ostranauts.Blueprints;

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
public static class Patch_CondOwner_BlueprintsModeSwitch
{
	[HarmonyPrefix]
	private static void Prefix(CondOwner __instance)
	{
		if (__instance == null || __instance.GetComponent<Placeholder>() == null)
		{
			return;
		}

		if (!BlueprintRuntime.TryGetPlaceholderRotation(__instance.strID, out float rotation))
		{
			return;
		}

		BlueprintRuntime.ApplyPlacementRotation(__instance, rotation);
		Plugin.LogInfo(
			"Blueprint placeholder rotation restored before ModeSwitch: " +
			__instance.strName +
			" -> " + rotation.ToString("0.##") + " degrees."
		);
	}

	[HarmonyPostfix]
	private static void Postfix(CondOwner __instance, CondOwner coNew)
	{
		if (__instance == null || coNew == null || __instance.GetComponent<Placeholder>() == null)
		{
			return;
		}

		if (!BlueprintRuntime.TryGetPlaceholderRotation(__instance.strID, out float rotation))
		{
			return;
		}

		BlueprintRuntime.ApplyPlacementRotation(coNew, rotation);
		BlueprintRuntime.ClearPlaceholderRotation(__instance.strID);
		Item newItem = coNew.GetComponent<Item>();
		Plugin.LogInfo(
			"Blueprint final ModeSwitch rotation applied: " +
			__instance.strName +
			" -> " + coNew.strName +
			" at " + rotation.ToString("0.##") +
			" degrees (new transform=" + coNew.transform.rotation.eulerAngles.z.ToString("0.##") +
			", new item=" + (newItem != null ? newItem.fLastRotation.ToString("0.##") : "n/a") + ")."
		);
	}
}
