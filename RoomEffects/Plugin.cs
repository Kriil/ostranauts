using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;

namespace Ostranauts.RoomEffects;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "Room_Effects";

	public const string PluginName = "Room Effects";

	public const string PluginVersion = "1.0.5";

	public static ConfigEntry<float> EngineeringWorkBonus;
	public static ConfigEntry<bool> EnforceWorkSpeedCap;
	public static ConfigEntry<float> TowingSecureSpeedBonus;
	public static ConfigEntry<float> WellnessFitnessBonus;
	public static ConfigEntry<float> WellnessStrengthBonus;
	public static ConfigEntry<float> RecreationPositiveBonus;
	public static ConfigEntry<float> RecreationNegativeReduction;
	public static ConfigEntry<float> BasicSleepBonus;
	public static ConfigEntry<float> LuxurySleepBonus;
	public static ConfigEntry<float> BathroomSpeedBonus;
	public static ConfigEntry<float> GalleyFoodRateReduction;
	public static ConfigEntry<float> GalleyFoodRateDurationHours;
	public static ConfigEntry<float> GalleyHydrationRateReduction;
	public static ConfigEntry<float> GalleyHydrationRateDurationHours;
	public static ConfigEntry<float> PassengerSmallRelaxBonus;
	public static ConfigEntry<float> PassengerMediumRelaxBonus;
	public static ConfigEntry<bool> DebugLoggingToggle;

	private Harmony _harmony;

	private void Awake()
	{
		EngineeringWorkBonus = Config.Bind(
			"Engineering Room",
			"EngineeringWorkBonus",
			0.1f,
			"Ship-wide work speed bonus (as decimal fraction) added when the ship has at least one Engineering room."
		);
		EnforceWorkSpeedCap = Config.Bind(
			"Engineering Room",
			"EnforceWorkSpeedCap",
			false,
			"Whether to enforce the vanilla work speed cap in Engineering rooms."
		);
		TowingSecureSpeedBonus = Config.Bind(
			"Towing Room",
			"TowingSecureSpeedBonus",
			0.5f,
			"Progress bonus (as decimal fraction) applied while securing a tow brace in a Towing room."
		);
		WellnessFitnessBonus = Config.Bind(
			"Wellness Room",
			"WellnessFitnessBonus",
			0.5f,
			"Training bonus (as decimal fraction) applied to treadmill use in a Wellness room."
		);
		WellnessStrengthBonus = Config.Bind(
			"Wellness Room",
			"WellnessStrengthBonus",
			0.5f,
			"Training bonus (as decimal fraction) applied to strength trainer use in a Wellness room."
		);
		RecreationPositiveBonus = Config.Bind(
			"Recreation Room",
			"RecreationPositiveBonus",
			0.25f,
			"Bonus (as decimal fraction) applied to positive interaction effects while in a Recreation room."
		);
		RecreationNegativeReduction = Config.Bind(
			"Recreation Room",
			"RecreationNegativeReduction",
			0.25f,
			"Bonus (as decimal fraction) applied to negative interaction effects while in a Recreation room."
		);
		BasicSleepBonus = Config.Bind(
			"Quarters",
			"BasicSleepBonus",
			0.25f,
			"Sleep efficiency bonus (as decimal fraction) applied while sleeping in Basic Quarters."
		);
		LuxurySleepBonus = Config.Bind(
			"Quarters",
			"LuxurySleepBonus",
			0.5f,
			"Sleep efficiency bonus (as decimal fraction) applied while sleeping in Luxury Quarters."
		);
		BathroomSpeedBonus = Config.Bind(
			"Bathroom",
			"BathroomSpeedBonus",
			0.5f,
			"Speed bonus (as decimal fraction) applied to defecation and cleansing actions in a Bathroom."
		);
		GalleyFoodRateReduction = Config.Bind(
			"Galley",
			"GalleyFoodRateReduction",
			0.25f,
			"Hunger gain reduction bonus (as decimal fraction) applied after eating in a Galley."
		);
		GalleyFoodRateDurationHours = Config.Bind(
			"Galley",
			"GalleyFoodRateDurationHours",
			2.0f,
			"Duration in hours of the hunger gain reduction after eating in a Galley."
		);
		GalleyHydrationRateReduction = Config.Bind(
			"Galley",
			"GalleyHydrationRateReduction",
			0.25f,
			"Thirst gain reduction bonus (as decimal fraction) applied after drinking water in a Galley."
		);
		GalleyHydrationRateDurationHours = Config.Bind(
			"Galley",
			"GalleyHydrationRateDurationHours",
			2.0f,
			"Duration in hours of the thirst gain reduction after drinking water in a Galley."
		);
		PassengerSmallRelaxBonus = Config.Bind(
			"Passenger Room",
			"PassengerSmallRelaxBonus",
			0.25f,
			"Relaxation bonus (as decimal fraction) applied while relaxing in chairs in a Small Passenger Room."
		);
		PassengerMediumRelaxBonus = Config.Bind(
			"Passenger Room",
			"PassengerMediumRelaxBonus",
			0.4f,
			"Relaxation bonus (as decimal fraction) applied while relaxing in chairs in a Medium Passenger Room."
		);
		DebugLoggingToggle = Config.Bind(
			"Debug",
			"DebugLoggingToggle",
			false,
			"Set to true to enable debug logging for Room Effects. Warning: may produce a large amount of log output."
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

