using HarmonyLib;
using UnityEngine;

namespace Kriil.Ostranauts.RoomEffects;

internal static class RoomEffectUtils
{
	// Rebuilds all room-local cached effect stats for the given room.
	public static void RefreshRoomBonuses(Room room)
	{
		if (room?.CO == null)
		{
			return;
		}

		double airPumpBonus = 0.0;
		double heatBonus = 0.0;
		double coolBonus = 0.0;

		string roomSpecName = room.GetRoomSpec()?.strName ?? "Blank";

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
	}

	private static void ApplyEngineeringBonuses(Room room, ref double airPumpBonus, ref double heatBonus, ref double coolBonus)
	{
		if (room.aCos == null)
		{
			return;
		}

		foreach (CondOwner co in room.aCos)
		{
			if (co == null || !co.HasCond("IsInstalled"))
			{
				continue;
			}

			if (co.HasCond("IsAirPump"))
			{
				airPumpBonus = Plugin.EngineeringAirPumpBonus.Value;
			}

			if (co.HasCond("IsHeater"))
			{
				heatBonus = Plugin.EngineeringHeatBonus.Value;
			}

			if (co.HasCond("IsCooler"))
			{
				coolBonus = Plugin.EngineeringCoolBonus.Value;
			}
		}
	}

	private static void ApplyAirlockBonuses(Room room, ref double airPumpBonus, ref double heatBonus, ref double coolBonus)
	{
		// Reserved for future airlock-specific device bonuses.
	}

	// Checks whether the ship has an engineering room and updates the ship-wide work bonus.
	public static void SetShipEngineeringWorkBonus(Room room)
	{
		Ship ship = room?.CO?.ship;
		CondOwner shipCo = ship?.ShipCO;
		if (ship == null || shipCo == null || ship.aRooms == null)
		{
			return;
		}

		bool hasEngineering = false;

		foreach (Room shipRoom in ship.aRooms)
		{
			if (shipRoom == null || shipRoom.Void)
			{
				continue;
			}

			var spec = shipRoom.GetRoomSpec();
			if (spec != null && spec.strName == "Engineering")
			{
				hasEngineering = true;
				break;
			}
		}

		shipCo.SetCondAmount("StatShipEngineeringWorkBonus", hasEngineering ? Plugin.EngineeringWorkBonus.Value : 0.0, 0.0);
	}
}
