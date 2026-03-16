using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using UnityEngine;

namespace Ostranauts.ConstructionTweaks;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "Construction_Tweaks";
	public const string PluginName = "Construction Tweaks";
	public const string PluginVersion = "1.1.2";
	public const string PlaceholderWallConditionName = "TIsPlaceholderWall1x1";
	public const string PlaceholderWallAddLootName = "CTTILPlaceholderWallAdds";
	public static ConfigEntry<bool> KeepInventoryOpenOnInstall;
	public static ConfigEntry<bool> AltClickInstallShortcut;
	public static ConfigEntry<bool> DragSelectionOverlay;
	public static ConfigEntry<bool> PlaceholderWallSockets;
	internal static ManualLogSource Log;
	private Harmony _harmony;


	private void Awake()
	{
		Log = Logger;
		KeepInventoryOpenOnInstall = Config.Bind(
			"General",
			"KeepInventoryOpenOnInstall",
			true,
			"If true, the inventory will remain open after installing a module or item. If false, the inventory will close as normal."
		);
		AltClickInstallShortcut = Config.Bind(
			"General",
			"AltClickInstallShortcut",
			true,
			"If true, Alt+left-clicking an installable inventory item starts placement immediately."
		);
		DragSelectionOverlay = Config.Bind(
			"General",
			"DragSelectionOverlay",
			true,
			"If true, drag-select shows tile highlights and a dimensions label. If false, the vanilla drag selection behavior is unchanged."
		);
		PlaceholderWallSockets = Config.Bind(
			"General",
			"PlaceholderWallSockets",
			true,
			"If true, wall placeholders get the custom socket and wall-passability behavior. If false, placeholder walls use vanilla behavior."
		);
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo("Applied Harmony patches for Construction Tweaks.");
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}

	internal static void LogPatchException(string context, Exception ex)
	{
		Debug.LogError($"[{PluginGuid}] Exception in {context}: {ex}");
		Log?.LogError($"Exception in {context}: {ex}");
	}
}

