using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;

namespace Kriil.Ostranauts.RoomEffects;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "kriil.ostranauts.roomeffects";

	public const string PluginName = "Room Effects";

	public const string PluginVersion = "0.1.0";

	public static ConfigEntry<float> EngineeringWorkBonus;

	private Harmony _harmony;

	private void Awake()
	{
		EngineeringWorkBonus = Config.Bind(
			"Room Effects",
			"EngineeringWorkBonus",
			1.0f,
			"Ship-wide work-rate bonus added when the ship has at least one Engineering room."
		);

		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo("Applied Harmony patches for Room Effects.");
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}
