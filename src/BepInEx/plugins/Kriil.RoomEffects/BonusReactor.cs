namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusReactor
{
	private const string CondRoomThrusterBonus = "StatRoomReactorThrusterBonus";
	private const string CondRoomIntakeBonus = "StatRoomReactorIntakeBonus";

	public static void ApplyBonuses(Room room)
	{	
		float reactorThrusterBonus = 0f;
		float reactorIntakeBonus = 0f;

		bool hasThruster = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsRCSClusterInstalled"), "Power");
		bool hasIntake = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsRCSDistroInstalled"), "Power");

		if (hasThruster)
		{
			reactorThrusterBonus = Plugin.ReactorThrusterBonus.Value;
			RoomEffectUtils.LogRoomEffect($"Applied Reactor Thruster Bonus of {reactorThrusterBonus * 100f}% due to RCS Cluster device installed in room.", "Reactor", room);
		} else {
			RoomEffectUtils.LogRoomEffect($"No thruster installed in room, setting thruster bonus to 0%.", "Reactor", room);
		}
		if (hasIntake)		{
			reactorIntakeBonus = Plugin.ReactorIntakeEfficiencyBonus.Value;
			RoomEffectUtils.LogRoomEffect($"Applied Reactor Intake Efficiency Bonus of {reactorIntakeBonus * 100f}% due to RCS Distribution device installed in room.", "Reactor", room);
		} else {
			RoomEffectUtils.LogRoomEffect($"No intake device installed in room, setting intake bonus to 0%.", "Reactor", room);
		}
		room.CO.SetCondAmount(CondRoomThrusterBonus, reactorThrusterBonus, 0.0);
		room.CO.SetCondAmount(CondRoomIntakeBonus, reactorIntakeBonus, 0.0);
	}

	public static double GetThrusterBonus(Ship ship)
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

			float roomBonus = (float)RoomEffectUtils.GetRoomCondAmount(room, CondRoomThrusterBonus);
			bonus += roomBonus;
			if (roomBonus != 0f)
			{
				RoomEffectUtils.LogRoomEffect($"Applying thruster bonus of {roomBonus * 100f}%", "Reactor", room);
			}
		}

		return bonus;
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
			if (roomBonus != 0f)
			{
				RoomEffectUtils.LogRoomEffect($"Applying intake bonus of {roomBonus * 100f}%", "Reactor", room);
			}
		}

		return bonus;
	}
}
