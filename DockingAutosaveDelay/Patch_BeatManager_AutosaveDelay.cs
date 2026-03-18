using System;
using HarmonyLib;
using Ostranauts.Core;

namespace Ostranauts.DockingAutosaveDelay;

[HarmonyPatch(typeof(BeatManager), nameof(BeatManager.Update))]
internal static class Patch_BeatManager_AutosaveDelay
{
	private static readonly AccessTools.FieldRef<double> AutosaveRemainRef =
		AccessTools.StaticFieldRefAccess<double>(AccessTools.Field(typeof(BeatManager), "fAutosaveRemain"));

	private static double _lastLoggedEpoch = double.NegativeInfinity;

	private static void Prefix(double fTimeElapsed)
	{
		try
		{
			if (!LoadManager.IsAutoSaveEnabled)
			{
				return;
			}

			if (CrewSim.bShipEdit || CrewSim.bShipEditTest || CrewSim.Paused || CrewSim.bUILock)
			{
				return;
			}

			if (CrewSim.CanvasManager != null && CrewSim.CanvasManager.State == CanvasManager.GUIState.SOCIAL)
			{
				return;
			}

			if (!IsDockingModeActive())
			{
				return;
			}

			double autosaveRemain = AutosaveRemainRef();
			if (autosaveRemain - fTimeElapsed >= 0.0)
			{
				return;
			}

			AutosaveRemainRef() = Plugin.DockingDelaySeconds + fTimeElapsed;
			LogDelayOncePerSecond();
		}
		catch (Exception ex)
		{
			Plugin.LogException("BeatManager.Update autosave delay prefix", ex);
		}
	}

	private static bool IsDockingModeActive()
	{
		return GUIDockSys.instance != null && GUIDockSys.instance.bActive;
	}

	private static void LogDelayOncePerSecond()
	{
		double epoch = StarSystem.fEpoch;
		if (epoch - _lastLoggedEpoch < 1.0)
		{
			return;
		}

		_lastLoggedEpoch = epoch;
		Plugin.LogInfo($"[{Plugin.PluginGuid}] Delayed periodic autosave by {Plugin.DockingDelaySeconds:0} seconds because docking mode is active.");
	}
}
