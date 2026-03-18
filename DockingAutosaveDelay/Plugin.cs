using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Ostranauts.DockingAutosaveDelay;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "Docking_Autosave_Delay";
	public const string PluginName = "Docking Autosave Delay";
	public const string PluginVersion = "1.0.0";
	public const double DockingDelaySeconds = 60.0;

	internal static ManualLogSource Log;

	private Harmony _harmony;

	private void Awake()
	{
		Log = Logger;
		Logger.LogInfo($"[{PluginGuid}] Initializing {PluginName} v{PluginVersion}.");

		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll();
			Logger.LogInfo($"[{PluginGuid}] Harmony patches applied successfully.");
		}
		catch (Exception ex)
		{
			LogException("plugin startup", ex);
			throw;
		}
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}

	internal static void LogInfo(string message)
	{
		Log?.LogInfo(message);
	}

	internal static void LogException(string context, Exception ex)
	{
		Debug.LogError($"[{PluginGuid}] Exception during {context}: {ex}");
		Log?.LogError($"Exception during {context}: {ex}");
	}
}
