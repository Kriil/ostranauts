using HarmonyLib;

namespace Ostranauts.Blueprints;

[HarmonyPatch(typeof(CrewSim), "MouseHandler")]
public static class Patch_CrewSim_BlueprintsMouse
{
	[HarmonyPrefix]
	private static bool Prefix(CrewSim __instance)
	{
		if (!BlueprintRuntime.IsActive)
		{
			return true;
		}

		BlueprintRuntime.HandleMouse(__instance);
		return false;
	}
}

[HarmonyPatch(typeof(CrewSim), "Update")]
public static class Patch_CrewSim_BlueprintsUpdate
{
	[HarmonyPostfix]
	private static void Postfix(CrewSim __instance)
	{
		BlueprintRuntime.Tick(__instance);
	}
}

[HarmonyPatch(typeof(CommandRotateItem), "Execute")]
public static class Patch_CommandRotateItem_Blueprints
{
	[HarmonyPostfix]
	private static void Postfix(CommandRotateItem __instance)
	{
		if (__instance != null && __instance.Down && BlueprintRuntime.IsPlacing)
		{
			BlueprintRuntime.RotatePlacement();
		}
	}
}
