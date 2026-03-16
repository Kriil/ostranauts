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
	public const string PluginVersion = "0.1.18";

	public static ConfigEntry<string> BlueprintDirectory;
	public static ConfigEntry<string> BlueprintFilePrefix;
	internal static ManualLogSource Log;
	private static Texture2D _blueprintActionTexture;

	private Harmony _harmony;

	private void Awake()
	{
		Log = Logger;
		BlueprintDirectory = Config.Bind(
			"Storage",
			"BlueprintDirectory",
			System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.CurrentDirectory, "blueprints")),
			"Directory where blueprint JSON files are written."
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

	internal static Texture2D EnsureBlueprintActionTextureLoaded()
	{
		const string imageName = "GUIActionBlueprint.png";
		if (DataHandler.dictImages != null &&
			DataHandler.dictImages.TryGetValue(imageName, out Texture2D cachedTexture) &&
			cachedTexture != null &&
			cachedTexture.name != "missing.png")
		{
			return cachedTexture;
		}

		if (_blueprintActionTexture != null)
		{
			if (DataHandler.dictImages != null)
			{
				DataHandler.dictImages[imageName] = _blueprintActionTexture;
			}
			return _blueprintActionTexture;
		}

		string imagePath = Path.Combine(Environment.CurrentDirectory, "Ostranauts_Data");
		imagePath = Path.Combine(imagePath, "Mods");
		imagePath = Path.Combine(imagePath, PluginName);
		imagePath = Path.Combine(imagePath, "images");
		imagePath = Path.Combine(imagePath, imageName);
		if (!File.Exists(imagePath))
		{
			LogWarning("Blueprint action image file was not found at: " + imagePath);
			return null;
		}

		try
		{
			byte[] data = File.ReadAllBytes(imagePath);
			Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.LoadImage(data);
			texture.name = imageName;
			_blueprintActionTexture = texture;
			if (DataHandler.dictImages != null)
			{
				DataHandler.dictImages[imageName] = texture;
			}
			LogInfo("Registered Blueprint action image from " + imagePath);
			return texture;
		}
		catch (Exception ex)
		{
			LogException("EnsureBlueprintActionTextureLoaded", ex);
			return null;
		}
	}
}





