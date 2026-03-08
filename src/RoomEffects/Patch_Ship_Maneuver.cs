using HarmonyLib;

namespace Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Ship), nameof(Ship.Maneuver))]
internal static class Patch_Ship_Maneuver
{
	public static void Prefix(Ship __instance, ref float fX, ref float fY, ref float fR, int nNoiseOnly, float fDeltaTime, Ship.EngineMode engineMode)
	{
		if (!RoomEffectUtils.IsPlayerShip(__instance) || engineMode == Ship.EngineMode.ROTOR)
		{
			return;
		}

		double bonus = BonusReactor.GetThrusterBonus(__instance);
		if (bonus <= 0.0)
		{
			return;
		}

		RoomEffectUtils.LogRoomEffect($"Applying thruster bonus of {bonus * 100f}% to ship maneuvering.", "Engineering", null);
		float multiplier = 1f + (float)bonus;
		fX *= multiplier;
		fY *= multiplier;
		fR *= multiplier;
	}
}
