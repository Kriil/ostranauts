namespace Ostranauts.RoomEffects;

internal static class BonusReactor
{
	private const string CondShipThrusterBonus = "StatShipReactorThrusterBonus";
	private const string CondRoomIntakeBonus = "StatRoomReactorIntakeBonus";

	public static void ApplyBonuses(Room room)
	{	
		float reactorIntakeBonus = 0f;
		bool hasIntake = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsRCSDistroInstalled"), "Power");

		if (hasIntake)
		{
			reactorIntakeBonus = Plugin.ReactorIntakeEfficiencyBonus.Value;
			RoomEffectUtils.LogRoomEffect($"Setting Reactor Intake Efficiency Bonus of {reactorIntakeBonus * 100f}% due to RCS Distribution device installed in room.", "Reactor", room);
		}
		else
		{
			RoomEffectUtils.LogRoomEffect($"No intake device installed in room, setting intake bonus to 0%.", "Reactor", room);
		}

		room.CO.SetCondAmount(CondRoomIntakeBonus, reactorIntakeBonus, 0.0);
	}

	public static void ApplyShipBonuses(Ship ship, CondOwner shipCo)
	{
		bool hasReactorRoom = false;
		foreach (Room shipRoom in ship.aRooms)
		{
			if (shipRoom == null || shipRoom.Void)
			{
				continue;
			}

			if (shipRoom.GetRoomSpec()?.strName == "Reactor")
			{
				hasReactorRoom = true;
				break;
			}
		}

		CondTrigger thrusterTrigger = DataHandler.GetCondTrigger("TIsRCSClusterInstalled");
		bool hasThruster = ship.GetCOs(thrusterTrigger, true, false, false).Count > 0;

		float reactorThrusterBonus = 0f;
		if (hasReactorRoom && hasThruster)
		{
			reactorThrusterBonus = Plugin.ReactorThrusterBonus.Value;
			RoomEffectUtils.LogRoomEffect($"Setting ship-wide reactor thruster bonus of {reactorThrusterBonus * 100f}% due to Reactor room and RCS Cluster presence.", "Reactor", null);
		}
		else
		{
			RoomEffectUtils.LogRoomEffect($"Reactor thruster requirements not met (reactorRoom={hasReactorRoom}, thrusterInstalled={hasThruster}), setting bonus to 0%.", "Reactor", null);
		}

		shipCo.SetCondAmount(CondShipThrusterBonus, reactorThrusterBonus, 0.0);
	}

	public static double GetThrusterBonus(Ship ship)
	{
		if (ship?.ShipCO == null)
		{
			return 0.0;
		}

		return ship.ShipCO.GetCondAmount(CondShipThrusterBonus);
	}

	public static double GetIntakeBonus(Ship ship)
	{
		if (ship?.aRooms == null)
		{
			return 0.0;
		}

		double bonus = 0.0;
		foreach (Room room in ship.aRooms)
		{
			if (room == null || room.Void)
			{
				continue;
			}

			float roomBonus = (float)RoomEffectUtils.GetRoomCondAmount(room, CondRoomIntakeBonus);
			bonus += roomBonus;
		}

		return bonus;
	}
}
