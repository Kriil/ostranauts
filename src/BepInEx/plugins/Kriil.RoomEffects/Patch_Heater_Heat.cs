using HarmonyLib;
using UnityEngine;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Heater), "Heat")]
internal static class Patch_Heater_Heat
{
	public static void Prefix(Heater __instance, ref double fTimePassed)
	{
		CondOwner co = __instance.GetComponent<CondOwner>();
		if (co?.ship == null || !RoomEffectUtils.IsPlayerShip(co.ship))
		{
			return;
		}

		double bonus = 0.0;
		Room targetRoom = null;

		CondTrigger heaterInstalled = DataHandler.GetCondTrigger("TIsHeater01Installed");
		CondTrigger coolerInstalled = DataHandler.GetCondTrigger("TIsCooler01Installed");
		bool isHeaterInstalled = heaterInstalled != null && heaterInstalled.Triggered(co, null, true);
		bool isCoolerInstalled = coolerInstalled != null && coolerInstalled.Triggered(co, null, true);

		if (isHeaterInstalled)
		{
			targetRoom = GetHeaterTargetRoom(__instance, co);
			if (targetRoom?.CO == null)
			{
				return;
			}
			bonus = targetRoom.CO.GetCondAmount("StatRoomHeatSpeedBonus");
		}
		else if (isCoolerInstalled)
		{
			targetRoom = GetHeaterTargetRoom(__instance, co);
			if (targetRoom?.CO == null)
			{
				return;
			}
			bonus = targetRoom.CO.GetCondAmount("StatRoomCoolSpeedBonus");
		}
		else
		{
			return;
		}

		if (bonus == 0.0)
		{
			return;
		}

		fTimePassed *= 1.0 + bonus;
	}

	private static Room GetHeaterTargetRoom(Heater heater, CondOwner co)
	{
		return RoomEffectUtils.GetRoomAtPoint(co, "use");
	}
}
