using HarmonyLib;

namespace Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Ship), nameof(Ship.RemoveGasMass))]
internal static class Patch_Ship_RemoveGasMass
{
	public static void Prefix(Ship __instance, ref float fMassNeeded)
	{
		if (!RoomEffectUtils.IsPlayerShip(__instance))
		{
			return;
		}

		double bonus = BonusReactor.GetIntakeBonus(__instance);
		if (bonus <= 0.0)
		{
			return;
		}

		RoomEffectUtils.LogRoomEffect($"Applying intake bonus of {bonus * 100f}% to ship gas intake.", "Engineering", null);
		fMassNeeded /= 1f + (float)bonus;
	}
}
