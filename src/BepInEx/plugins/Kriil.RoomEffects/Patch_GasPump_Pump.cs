using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

// Applies the air pump speed bonus from room effects to gas pumps.
[HarmonyPatch(typeof(GasPump), "Pump")]
internal static class Patch_GasPump_Pump
{
	public static void Prefix(GasPump __instance, ref float fCoeff)
	{
		if (__instance == null)
		{
			return;
		}

		CondOwner co = __instance.GetComponent<CondOwner>();
		if (co?.currentRoom?.CO == null)
		{
			return;
		}

		float bonus = (float)co.currentRoom.CO.GetCondAmount("StatRoomAirPumpSpeedBonus");
		if (bonus == 0f)
		{
			return;
		}

		fCoeff *= 1f + bonus;
	}
}