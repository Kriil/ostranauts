using HarmonyLib;
using Ostranauts.UI.PDA;
using UnityEngine;

namespace Ostranauts.Blueprints;

[HarmonyPatch(typeof(GUIPDA), "ShowJobPaintUI")]
public static class Patch_GUIPDA_BlueprintsShowJobPaintUI
{
	private const string BlueprintButtonName = "GUIJobItem_Blueprint";
	private static readonly AccessTools.FieldRef<GUIPDA, GUIJobItem> PrefabGUIJobItemRef =
		AccessTools.FieldRefAccess<GUIPDA, GUIJobItem>("prefabGUIJobItem");

	[HarmonyPostfix]
	private static void Postfix(GUIPDA __instance, string btn)
	{
		if (__instance == null || btn != "actions")
		{
			return;
		}

		GameObject jobTypes = __instance.goJobTypes;
		if (jobTypes == null)
		{
			Plugin.LogWarning("Blueprint PDA injection skipped: goJobTypes was null.");
			return;
		}

		Transform parent = jobTypes.transform;
		if (parent.Find(BlueprintButtonName) != null)
		{
			Plugin.LogInfo("Blueprint PDA button already present for this rebuild; skipping duplicate insert.");
			return;
		}

		GUIJobItem prefab = PrefabGUIJobItemRef(__instance);
		if (prefab == null)
		{
			Plugin.LogWarning("Blueprint PDA injection skipped: prefabGUIJobItem was null.");
			return;
		}

		Texture2D icon = Plugin.EnsureBlueprintActionTextureLoaded();
		if (icon == null || icon.name == "missing.png")
		{
			Plugin.LogWarning("Blueprint PDA injection could not resolve GUIActionBlueprint.png.");
		}

		GUIJobItem blueprintButton = Object.Instantiate(prefab, parent);
		blueprintButton.name = BlueprintButtonName;
		blueprintButton.SetData("BLUE", "GUIActionBlueprint", BlueprintRuntime.StartSelectionModeFromPda);
		Plugin.LogInfo("Inserted Blueprint PDA action button after the vanilla jobs actions.");
	}
}
