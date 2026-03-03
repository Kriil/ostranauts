using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace Kriil.Ostranauts.RoomEffects;

internal static class RoomEffectUtils
{
	public static void RefreshRoomBonuses(Room room)
	{
		if (room?.CO?.ship == null)
		{
			return;
		}

		if (!IsPlayerShip(room.CO.ship))
		{
			return;
		}

		string shipName = room.CO.ship.strRegID ?? "Blank";
		string roomSpecName = room.GetRoomSpec()?.strName ?? "Blank";
		string roomId = room.CO.strID ?? "null";

		double airPumpBonus = 0.0;
		double heatBonus = 0.0;
		double coolBonus = 0.0;

		switch (roomSpecName)
		{
			case "Engineering":
				ApplyEngineeringBonuses(room, ref airPumpBonus, ref heatBonus, ref coolBonus);
				break;
			case "Airlock":
				ApplyAirlockBonuses(room, ref airPumpBonus, ref heatBonus, ref coolBonus);
				break;
		}

		room.CO.SetCondAmount("StatRoomAirPumpSpeedBonus", airPumpBonus, 0.0);
		room.CO.SetCondAmount("StatRoomHeatSpeedBonus", heatBonus, 0.0);
		room.CO.SetCondAmount("StatRoomCoolSpeedBonus", coolBonus, 0.0);

		if (airPumpBonus != 0.0)
		{
			Debug.Log($"[kriil.ostranauts.roomeffects] Setting air pump speed bonus of {airPumpBonus * 100.0}% for room '{roomSpecName}-{roomId}' in ship '{shipName}'.");
		}
		if (heatBonus != 0.0)
		{
			Debug.Log($"[kriil.ostranauts.roomeffects] Setting heater speed bonus of {heatBonus * 100.0}% for room '{roomSpecName}-{roomId}' in ship '{shipName}'.");
		}
		if (coolBonus != 0.0)
		{
			Debug.Log($"[kriil.ostranauts.roomeffects] Setting cooler speed bonus of {coolBonus * 100.0}% for room '{roomSpecName}-{roomId}' in ship '{shipName}'.");
		}
	}

	public static void RefreshShipWideBonuses(Room room)
	{
		Ship ship = room.CO?.ship;
		CondOwner shipCo = ship?.ShipCO;
		if (ship == null || shipCo == null || ship.aRooms == null)
		{
			return;
		}

		if (!IsPlayerShip(ship))
		{
			return;
		}

		ApplyShipEngineeringWorkBonus(ship);
	}

	private static void ApplyEngineeringBonuses(Room room, ref double airPumpBonus, ref double heatBonus, ref double coolBonus)
	{
		CondTrigger airPumpTrigger = DataHandler.GetCondTrigger("TIsAirPump02Installed");
		CondTrigger heaterTrigger = DataHandler.GetCondTrigger("TIsHeater01Installed");
		CondTrigger coolerTrigger = DataHandler.GetCondTrigger("TIsCooler01Installed");

		if (HasInstalledDeviceInRoomByPoint(room, airPumpTrigger, "GasInput"))
		{
			airPumpBonus = Plugin.EngineeringAirPumpBonus.Value;
		}
		if (HasInstalledDeviceInRoomByPoint(room, heaterTrigger, "use"))
		{
			heatBonus = Plugin.EngineeringHeatBonus.Value;
		}
		if (HasInstalledDeviceInRoomByPoint(room, coolerTrigger, "use"))
		{
			coolBonus = Plugin.EngineeringCoolBonus.Value;
		}
	}

	private static bool HasInstalledDeviceInRoomByPoint(Room room, CondTrigger trigger, string pointName)
	{
		if (room?.CO?.ship == null || trigger == null)
		{
			return false;
		}

		foreach (CondOwner co in room.CO.ship.GetCOs(trigger, false, false, false))
		{
			Room pointRoom = GetRoomAtPoint(co, pointName);
			if (pointRoom == room)
			{
				return true;
			}
		}

		return false;
	}

	public static Room GetRoomAtPoint(CondOwner co, string pointName)
	{
		if (co?.ship == null || string.IsNullOrEmpty(pointName) || pointName == "ignore")
		{
			return null;
		}

		Vector2 pos = co.GetPos(pointName, false);
		if (float.IsInfinity(pos.x) || float.IsInfinity(pos.y))
		{
			return null;
		}

		return co.ship.GetRoomAtWorldCoords1(pos, true);
	}

	private static void ApplyAirlockBonuses(Room room, ref double airPumpBonus, ref double heatBonus, ref double coolBonus)
	{
		// Reserved for future airlock-specific device bonuses.
	}

	public static void ApplyShipEngineeringWorkBonus(Ship ship)
	{
		bool hasEngineering = false;

		foreach (Room shipRoom in ship.aRooms)
		{
			if (shipRoom == null || shipRoom.Void)
			{
				continue;
			}

			RoomSpec spec = shipRoom.GetRoomSpec();
			if (spec != null && spec.strName == "Engineering")
			{
				hasEngineering = true;
				break;
			}
		}

		shipCo.SetCondAmount("StatShipEngineeringWorkBonus", hasEngineering ? Plugin.EngineeringWorkBonus.Value : 0.0, 0.0);
	}

	public static bool IsPlayerShip(Ship ship)
	{
		Debug.Log($"[kriil.ostranauts.roomeffects] Checking if ship '{ship?.strRegID ?? "null"}' is the player's ship: {(CrewSim.coPlayer?.ship?.strRegID ?? "null")}");
		return ship != null && CrewSim.coPlayer != null && CrewSim.coPlayer.ship == ship;
	}
}
