using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Ostranauts.Blueprints;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "Blueprints";
	public const string PluginName = "Blueprints";
	public const string PluginVersion = "0.3.8";

	public static ConfigEntry<bool> EnableBlueprintSaving;
	public static ConfigEntry<string> BlueprintDirectory;
	public static ConfigEntry<string> BlueprintFilePrefix;
	internal static ManualLogSource Log;

	private Harmony _harmony;

	private void Awake()
	{
		Log = Logger;
		string defaultBlueprintDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, "Ostranauts_Data");
		defaultBlueprintDirectory = System.IO.Path.Combine(defaultBlueprintDirectory, "Mods");
		defaultBlueprintDirectory = System.IO.Path.Combine(defaultBlueprintDirectory, PluginName);
		defaultBlueprintDirectory = System.IO.Path.Combine(defaultBlueprintDirectory, "saved_blueprints");
		BlueprintDirectory = Config.Bind(
			"Storage",
			"BlueprintDirectory",
			System.IO.Path.GetFullPath(defaultBlueprintDirectory),
			"Directory where blueprint JSON files are written."
		);
		EnableBlueprintSaving = Config.Bind(
			"Storage",
			"EnableBlueprintSaving",
			false,
			"When true, captured blueprints are written to JSON files and the Orders file-selection panel is shown."
		);
		BlueprintFilePrefix = Config.Bind(
			"Storage",
			"BlueprintFilePrefix",
			"blueprint",
			"Filename prefix used when writing blueprint JSON files."
		);

		BlueprintRuntime.Initialize();
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo("Applied Harmony patches for Blueprints.");
	}

	private void OnDestroy()
	{
		BlueprintRuntime.Shutdown();
		_harmony?.UnpatchSelf();
	}

	internal static void LogInfo(string message)
	{
		Log?.LogInfo(message);
	}

	internal static void LogWarning(string message)
	{
		Log?.LogWarning(message);
	}

	internal static void LogError(string message)
	{
		Log?.LogError(message);
	}

	internal static void LogException(string context, Exception ex)
	{
		Debug.LogError($"[{PluginGuid}] Exception in {context}: {ex}");
		Log?.LogError($"Exception in {context}: {ex}");
	}

	internal static bool IsBlueprintSavingEnabled()
	{
		return EnableBlueprintSaving != null && EnableBlueprintSaving.Value;
	}
}





























