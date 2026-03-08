using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;

namespace Ostranauts.RoomEffects;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "ostranauts.roomeffects";

	public const string PluginName = "Room Effects";

	public const string PluginVersion = "0.1.0";

	public static ConfigEntry<float> EngineeringWorkBonus;
	public static ConfigEntry<float> ReactorThrusterBonus;
	public static ConfigEntry<float> ReactorIntakeEfficiencyBonus;
	public static ConfigEntry<float> TowingSecureSpeedBonus;
	public static ConfigEntry<float> WellnessFitnessBonus;
	public static ConfigEntry<float> WellnessStrengthBonus;
	public static ConfigEntry<float> RecreationPositiveBonus;
	public static ConfigEntry<float> RecreationNegativeReduction;
	public static ConfigEntry<float> LuxurySleepBonus;
	public static ConfigEntry<float> BathroomSpeedBonus;
	public static ConfigEntry<float> GalleyFoodRateReduction;
	public static ConfigEntry<float> GalleyFoodRateDurationHours;
	public static ConfigEntry<float> GalleyHydrationRateReduction;
	public static ConfigEntry<float> GalleyHydrationRateDurationHours;
	public static ConfigEntry<float> BasicSleepBonus;
	public static ConfigEntry<float> PassengerSmallRelaxBonus;
	public static ConfigEntry<float> PassengerMediumRelaxBonus;

	private Harmony _harmony;

	private void Awake()
	{
		EngineeringWorkBonus = Config.Bind(
			"Room Effects",
			"EngineeringWorkBonus",
			1.0f,
			"Ship-wide work-rate bonus added when the ship has at least one Engineering room."
		);
		ReactorThrusterBonus = Config.Bind(
			"Room Effects",
			"ReactorThrusterBonus",
			0.25f,
			"Bonus applied to ship maneuvering thrust when the ship has thrusters installed in a Reactor room."
		);
		ReactorIntakeEfficiencyBonus = Config.Bind(
			"Room Effects",
			"ReactorIntakeEfficiencyBonus",
			0.2f,
			"Fuel-efficiency bonus applied to RCS gas usage when the ship has intakes installed in a Reactor room."
		);
		TowingSecureSpeedBonus = Config.Bind(
			"Room Effects",
			"TowingSecureSpeedBonus",
			0.5f,
			"Speed bonus applied while securing a tow brace in a Towing room."
		);
		WellnessFitnessBonus = Config.Bind(
			"Room Effects",
			"WellnessFitnessBonus",
			0.3f,
			"Training bonus applied to treadmill use in a Wellness room."
		);
		WellnessStrengthBonus = Config.Bind(
			"Room Effects",
			"WellnessStrengthBonus",
			0.3f,
			"Training bonus applied to strength trainer use in a Wellness room."
		);
		RecreationPositiveBonus = Config.Bind(
			"Room Effects",
			"RecreationPositiveBonus",
			0.25f,
			"Multiplier added to positive interaction effects while in a Recreation room."
		);
		RecreationNegativeReduction = Config.Bind(
			"Room Effects",
			"RecreationNegativeReduction",
			0.25f,
			"Percentage reduction applied to negative interaction effects while in a Recreation room."
		);
		LuxurySleepBonus = Config.Bind(
			"Room Effects",
			"LuxurySleepBonus",
			0.75f,
			"Sleep efficiency bonus applied while sleeping in Luxury Quarters."
		);
		BathroomSpeedBonus = Config.Bind(
			"Room Effects",
			"BathroomSpeedBonus",
			0.4f,
			"Speed bonus applied to defecation and cleansing actions in a Bathroom."
		);
		GalleyFoodRateReduction = Config.Bind(
			"Room Effects",
			"GalleyFoodRateReduction",
			0.25f,
			"Reduction applied to StatFoodRate after eating in a Galley."
		);
		GalleyFoodRateDurationHours = Config.Bind(
			"Room Effects",
			"GalleyFoodRateDurationHours",
			2.0f,
			"Duration in hours of the temporary StatFoodRate reduction after eating in a Galley."
		);
		GalleyHydrationRateReduction = Config.Bind(
			"Room Effects",
			"GalleyHydrationRateReduction",
			0.25f,
			"Reduction applied to StatHydrationRate after drinking water in a Galley."
		);
		GalleyHydrationRateDurationHours = Config.Bind(
			"Room Effects",
			"GalleyHydrationRateDurationHours",
			2.0f,
			"Duration in hours of the temporary StatHydrationRate reduction after drinking water in a Galley."
		);
		BasicSleepBonus = Config.Bind(
			"Room Effects",
			"BasicSleepBonus",
			0.4f,
			"Sleep efficiency bonus applied while sleeping in Basic Quarters."
		);
		PassengerSmallRelaxBonus = Config.Bind(
			"Room Effects",
			"PassengerSmallRelaxBonus",
			0.35f,
			"Relaxation bonus applied while using chairs in a small Passenger room."
		);
		PassengerMediumRelaxBonus = Config.Bind(
			"Room Effects",
			"PassengerMediumRelaxBonus",
			0.2f,
			"Relaxation bonus applied while using chairs in a medium Passenger room."
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
