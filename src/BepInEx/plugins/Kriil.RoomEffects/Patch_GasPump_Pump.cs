using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(GasPump), "Pump")]
internal static class Patch_GasPump_Pump
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

		// TODO: The bonus doesn't seem to apply unless player enters tthe room at least once while pumps are on.
		float bonus = (float)targetRoom.CO.GetCondAmount("StatRoomAirPumpSpeedBonus");
		RoomEffectUtils.LogRoomEffect($"Applying gas pump speed bonus of {bonus * 100.0}%.", "GasPump", targetRoom);
		if (bonus == 0f)
		{
			return;
		}

		float originalCoeff = fCoeff;
		fCoeff *= 1f + bonus;
	}
}
