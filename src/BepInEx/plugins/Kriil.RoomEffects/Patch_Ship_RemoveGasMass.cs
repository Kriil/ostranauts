using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

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

		fMassNeeded /= 1f + (float)bonus;
	}
}
