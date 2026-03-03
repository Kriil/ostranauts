using HarmonyLib;
using UnityEngine;

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

		float bonus = (float)targetRoom.CO.GetCondAmount("StatRoomAirPumpSpeedBonus");
		if (bonus == 0f)
		{
			return;
		}

		fCoeff *= 1f + bonus;
	}
}
