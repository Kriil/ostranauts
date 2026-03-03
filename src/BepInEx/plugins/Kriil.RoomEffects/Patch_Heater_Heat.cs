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
		string addPoint = Traverse.Create(heater).Field("strAddPoint").GetValue<string>();
		if (!string.IsNullOrEmpty(addPoint) && addPoint != "ignore")
		{
			Room room = RoomEffectUtils.GetRoomAtPoint(co, addPoint);
			if (room != null)
			{
				return room;
			}
		}

		string subPoint = Traverse.Create(heater).Field("strSubPoint").GetValue<string>();
		if (!string.IsNullOrEmpty(subPoint) && subPoint != "ignore")
		{
			Room room = RoomEffectUtils.GetRoomAtPoint(co, subPoint);
			if (room != null)
			{
				return room;
			}
		}

		return RoomEffectUtils.GetRoomAtPoint(co, "use");
	}
}
