using BepInEx;
using HarmonyLib;

namespace Ostranauts.ConstructionTweaks;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "Construction_Tweaks";
	public const string PluginName = "Construction Tweaks";
	public const string PluginVersion = "0.1.0";

	private Harmony _harmony;

	private void Awake()
	{
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo("Applied Harmony patches for Construction Tweaks.");
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}
