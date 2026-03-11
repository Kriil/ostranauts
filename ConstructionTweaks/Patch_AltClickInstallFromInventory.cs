using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ostranauts.ConstructionTweaks;

[HarmonyPatch(typeof(GUIInventoryItem), nameof(GUIInventoryItem.OnPointerDown))]
public static class Patch_AltClickInstallFromInventory
{
	[HarmonyPrefix]
	private static bool OnPointerDownPrefix(GUIInventoryItem __instance, PointerEventData eventData)
	{
		if (!Plugin.AltClickInstallShortcut.Value || !IsAltLeftClick(eventData))
		{
			return true;
		}

		if (GUIActionKeySelector.commandForceWalk.Held || global::GUIInventory.instance == null || global::GUIInventory.instance.Selected != null)
		{
			return true;
		}

		CondOwner item = __instance.CO;
		if (item == null || item.HasCond("IsSocialItem"))
		{
			return true;
		}

		JsonInstallable installable = FindInstallable(item);
		if (installable == null)
		{
			return true;
		}

		if (__instance.windowData != null)
		{
			__instance.windowData.SurfaceWindow();
		}

		installable.strPersistentCO = item.strID;
		global::GUIInventory.instance.JustClickedItem = true;
		CrewSim.objInstance.StartPaintingJob(installable);
		eventData.Use();
		return false;
	}

	private static bool IsAltLeftClick(PointerEventData eventData)
	{
		return eventData != null
			&& eventData.button == PointerEventData.InputButton.Left
			&& (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
	}

	private static JsonInstallable FindInstallable(CondOwner item)
	{
		if (item.aInteractions == null)
		{
			return null;
		}

		foreach (string interactionName in item.aInteractions)
		{
			Interaction interaction = DataHandler.GetInteraction(interactionName, null, false);
			if (interaction != null && !string.IsNullOrEmpty(interaction.strStartInstall))
			{
				string startInstall = interaction.strStartInstall;
				JsonCOOverlay overlay = DataHandler.GetCOOverlay(item.strCODef);
				if (overlay != null)
				{
					string modeSwitch = overlay.GetModeSwitch(startInstall);
					if (!string.IsNullOrEmpty(modeSwitch))
					{
						startInstall = modeSwitch;
					}
				}

				return Installables.GetJsonInstallable(startInstall);
			}
		}

		return null;
	}
}
