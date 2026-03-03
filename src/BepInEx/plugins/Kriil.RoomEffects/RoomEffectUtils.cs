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
				BonusEngineering.ApplyBonuses(room);
				break;
			case "Airlock":
				BonusAirlock.ApplyBonuses(room);
				break;
			case "Reactor":
				BonusReactor.ApplyBonuses(room);
				break;
			case "BridgeRoom":
				BonusBridge.ApplyBonuses(room);
				break;
			case "TowingRoom":
				BonusTowing.ApplyBonuses(room);
				break;
			case "WellnessRoom":
				BonusWellness.ApplyBonuses(room);
				break;
			case "Recreation":
				BonusRecreation.ApplyBonuses(room);
				break;
			case "LuxuryQuarters":
				BonusQuarters.ApplyBonuses(room, true);
				break;
			case "Bathroom":
				BonusBathroom.ApplyBonuses(room);
				break;
			case "Galley":
				BonusGalley.ApplyBonuses(room);
				break;
			case "BasicQuarters":
				BonusQuarters.ApplyBonuses(room, false);
				break;
			case "Passenger2":
				BonusPassenger.ApplyBonuses(room, false);
				break;
			case "Passenger1":
				// Note: Passenger1 is the "small" passenger room
				BonusPassenger.ApplyBonuses(room, true);
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

		BonusEngineering.ApplyShipBonuses(ship, shipCo);
	}

	public static bool HasInstalledDeviceInRoomByPoint(Room room, CondTrigger trigger, string pointName)
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

	public static bool IsPlayerShip(Ship ship)
	{
		return ship != null && CrewSim.coPlayer != null && CrewSim.coPlayer.ship == ship;
	}
}
