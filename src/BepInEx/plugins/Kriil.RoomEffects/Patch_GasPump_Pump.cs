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

		// TODO: The bonus doesn't seem to apply unless player enters tthe room at least once while pumps are on.
		float bonus = (float)targetRoom.CO.GetCondAmount("StatRoomAirPumpSpeedBonus");
		// UnityEngine.Debug.Log($"[kriil.ostranauts.roomeffects] Applying gas pump speed bonus from room '{targetRoom.GetRoomSpec()?.strName ?? "Blank"}-{targetRoom.CO.strID ?? "null"}'. Bonus: {bonus * 100.0}%.");
		if (bonus == 0f)
		{
			return;
		}

		// UnityEngine.Debug.Log($"[kriil.ostranauts.roomeffects] Original gas pump coefficient: {fCoeff}.");
		fCoeff *= 1f + bonus;
		// UnityEngine.Debug.Log($"[kriil.ostranauts.roomeffects] New gas pump coefficient after applying bonus: {fCoeff}.");
	}
}
