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
	public static ConfigEntry<float> EngineeringAirPumpBonus;
	public static ConfigEntry<float> EngineeringHeatBonus;
	public static ConfigEntry<float> EngineeringCoolBonus;

	private Harmony _harmony;

	private void Awake()
	{
		EngineeringWorkBonus = Config.Bind(
			"Room Effects",
			"EngineeringWorkBonus",
			1.0f,
			"Ship-wide work-rate bonus added when the ship has at least one Engineering room."
		);
		EngineeringAirPumpBonus = Config.Bind(
			"Room Effects",
			"EngineeringAirPumpBonus",
			1.0f,
			"Air pump bonus added when an air pump is installed in an Engineering room."
		);
		EngineeringHeatBonus = Config.Bind(
			"Room Effects",
			"EngineeringHeatBonus",
			1.0f,
			"Heat bonus added when a heater is installed in an Engineering room."
		);
		EngineeringCoolBonus = Config.Bind(
			"Room Effects",
			"EngineeringCoolBonus",
			1.0f,
			"Cool bonus added when a cooler is installed in an Engineering room."
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
