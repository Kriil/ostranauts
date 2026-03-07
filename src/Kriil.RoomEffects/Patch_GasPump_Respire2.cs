using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(GasPump), "Respire2")]
internal static class Patch_GasPump_Respire2
{
	public static void Prefix(GasPump __instance, ref float fCoeff)
	{
		CondOwner co = __instance.GetComponent<CondOwner>();
		if (co?.ship == null || !RoomEffectUtils.IsPlayerShip(co.ship))
		{
			return;
		}

		Room targetRoom = RoomEffectUtils.GetRoomAtPoint(co, "GasInput");
		if (targetRoom?.CO == null)
		{
			return;
		}

		double bonus = BonusAirlock.GetScrubberSpeedBonus(targetRoom);
		if (bonus <= 0.0)
		{
			return;
		}

		RoomEffectUtils.LogRoomEffect($"Applying airlock scrubber speed bonus of {bonus * 100f}% to scrubber pump.", "Airlock", targetRoom);
		fCoeff *= 1f + (float)bonus;
	}
}
