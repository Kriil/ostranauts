using HarmonyLib;

namespace Ostranauts.ConstructionTweaks;

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.InstallStart))]
public static class Patch_KeepInventoryOpenDuringInstall
{
	private static bool _suppressInventoryClose;

	[HarmonyPrefix]
	private static void InstallStartPrefix()
	{
		_suppressInventoryClose = CrewSim.inventoryGUI != null && CrewSim.inventoryGUI.IsOpen;
	}

	[HarmonyPostfix]
	private static void InstallStartPostfix()
	{
		_suppressInventoryClose = false;
	}

	[HarmonyPatch(typeof(CommandInventory), nameof(CommandInventory.ToggleInventory))]
	[HarmonyPrefix]
	private static bool ToggleInventoryPrefix(bool bForce)
	{
		if (!_suppressInventoryClose)
		{
			return true;
		}

		if (bForce || CrewSim.iaItmInstall == null || CrewSim.inventoryGUI == null || !CrewSim.inventoryGUI.IsOpen)
		{
			return true;
		}

		return false;
	}
}
