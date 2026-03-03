using System.ComponentModel;
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

		string roomSpecName = room.GetRoomSpec()?.strName ?? "Blank";

		switch (roomSpecName)
		{
			case "Engineering":
				ApplyEngineeringBonuses(room);
				break;
			case "Airlock":
				ApplyAirlockBonuses(room);
				break;
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

		ApplyShipEngineeringWorkBonus(ship, shipCo);
	}

	private static void ApplyEngineeringBonuses(Room room)
	{
		double airPumpBonus = 0.0;
		double heatBonus = 0.0;
		double coolBonus = 0.0;

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

		room.CO.SetCondAmount("StatRoomAirPumpSpeedBonus", airPumpBonus, 0.0);
		room.CO.SetCondAmount("StatRoomHeatSpeedBonus", heatBonus, 0.0);
		room.CO.SetCondAmount("StatRoomCoolSpeedBonus", coolBonus, 0.0);
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

	private static void ApplyAirlockBonuses(Room room)
	{
		// Reserved for future airlock-specific device bonuses.
	}

	public static void ApplyShipEngineeringWorkBonus(Ship ship, CondOwner shipCo)
	{
		bool hasEngineering = false;

		foreach (Room shipRoom in ship.aRooms)
		{
			if (shipRoom == null || shipRoom.Void)
			{
				continue;
			}

			string roomSpecName = shipRoom.GetRoomSpec()?.strName;
			if (roomSpecName == "Engineering")
			{
				hasEngineering = true;
				break;
			}
		}

		shipCo.SetCondAmount("StatShipEngineeringWorkBonus", hasEngineering ? Plugin.EngineeringWorkBonus.Value : 0.0, 0.0);
	}

	public static bool IsPlayerShip(Ship ship)
	{
		return ship != null && CrewSim.coPlayer != null && CrewSim.coPlayer.ship == ship;
	}
}
