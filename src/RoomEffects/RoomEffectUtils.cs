using UnityEngine;

namespace Ostranauts.RoomEffects;

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
		BonusReactor.ApplyShipBonuses(ship, shipCo);
	}

	public static bool HasInstalledDeviceInRoomByPoint(Room room, CondTrigger trigger, string pointName)
	{
		if (room?.CO?.ship == null || trigger == null)
		{
			return false;
		}

		foreach (CondOwner co in room.CO.ship.GetCOs(trigger, true, false, false))
		{
			Room deviceRoom = GetRoomAtPoint(co, pointName) ?? GetCondOwnerRoom(co);
			if (deviceRoom == room)
			{
				return true;
			}
		}

		return false;
	}

	public static int CountInstalledDevicesInRoomByPoint(Room room, CondTrigger trigger, string pointName)
	{
		if (room?.CO?.ship == null || trigger == null)
		{
			return 0;
		}

		int count = 0;
		foreach (CondOwner co in room.CO.ship.GetCOs(trigger, true, false, false))
		{
			Room deviceRoom = GetRoomAtPoint(co, pointName) ?? GetCondOwnerRoom(co);
			if (deviceRoom == room)
			{
				count++;
			}
		}

		return count;
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

	public static Room GetCondOwnerRoom(CondOwner co, string pointName = null)
	{
		if (co == null)
		{
			return null;
		}

		Room room = null;
		if (!string.IsNullOrEmpty(pointName))
		{
			room = GetRoomAtPoint(co, pointName);
		}

		if (room != null)
		{
			return room;
		}

		if (co.currentRoom != null)
		{
			return co.currentRoom;
		}

		if (co.ship == null || co.tf == null)
		{
			return null;
		}

		return co.ship.GetRoomAtWorldCoords1(new Vector2(co.tf.position.x, co.tf.position.y), true);
	}

	public static bool IsRoomSpec(Room room, string roomSpecName)
	{
		return room?.GetRoomSpec()?.strName == roomSpecName;
	}

	public static double GetRoomCondAmount(Room room, string condName)
	{
		if (room?.CO == null || string.IsNullOrEmpty(condName))
		{
			return 0.0;
		}

		return room.CO.GetCondAmount(condName);
	}

	public static double GetCondOwnerRoomBonus(CondOwner co, string condName)
	{
		return GetRoomCondAmount(GetCondOwnerRoom(co), condName);
	}

	public static double GetCondOwnerRoomBonus(Room room, string condName)
	{
		return GetRoomCondAmount(room, condName);
	}

	public static bool IsPlayerInteraction(Interaction interaction)
	{
		return interaction?.objUs != null && interaction.objUs == CrewSim.coPlayer;
	}

	public static float ApplySpeedBonus(float durationHours, double bonus)
	{
		if (durationHours <= 0f || bonus <= 0.0)
		{
			return durationHours;
		}

		return (float)(durationHours / (1.0 + bonus));
	}

	public static float ModifyQueuedInteractionDuration(Interaction interaction, float durationHours)
	{
		durationHours = BonusTowing.ModifyInteractionDuration(interaction, durationHours);
		durationHours = BonusBathroom.ModifyInteractionDuration(interaction, durationHours);
		return durationHours;
	}

	public static float ModifyInteractionTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, CondOwner coThem, float amount)
	{
		amount = BonusWellness.ModifyTriggerAmount(interaction, trigger, coUs, amount);
		amount = BonusRecreation.ModifyTriggerAmount(interaction, trigger, coUs, amount);
		amount = BonusQuarters.ModifyTriggerAmount(interaction, trigger, coUs, amount);
		amount = BonusGalley.ModifyTriggerAmount(interaction, trigger, coUs, coThem, amount);
		amount = BonusPassenger.ModifyTriggerAmount(interaction, trigger, coUs, amount);
		return amount;
	}

	public static double ModifyInteractionCondAmount(Interaction interaction, string condName, CondOwner coUs, CondOwner coThem, double amount)
	{
		amount = BonusWellness.ModifyCondAmount(interaction, condName, coUs, amount);
		amount = BonusRecreation.ModifyCondAmount(interaction, condName, coUs, amount);
		amount = BonusQuarters.ModifyCondAmount(interaction, condName, coUs, amount);
		amount = BonusGalley.ModifyCondAmount(interaction, condName, coUs, amount);
		amount = BonusPassenger.ModifyCondAmount(interaction, condName, coUs, amount);
		return amount;
	}



	public static bool IsPlayerShip(Ship ship)
	{
		if (ship == null || CrewSim.coPlayer == null)
		{
			return false;
		}

		if (CrewSim.coPlayer.OwnsShip(ship.strRegID))
		{
			return true;
		}

		return CrewSim.system != null && CrewSim.system.GetShipOwner(ship.strRegID) == CrewSim.coPlayer.strID;
	}

	public static string GetRoomIdentifier(Room room)
	{
		if (room == null)
		{
			return "null";
		}

		string roomId = room.CO.strID ?? "NoId";
		string roomSpecName = room.GetRoomSpec()?.strName ?? "NoSpec";
		return $"{roomSpecName}-{roomId}";
	}

	public static void LogRoomEffect(string message, string roomSpecName, Room room)
	{
		UnityEngine.Debug.Log($"[RoomEffects-{roomSpecName}] {message} (Room: {GetRoomIdentifier(room)})");
	}
}
