namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusReactor
{
	private const string CondRoomThrusterBonus = "StatRoomReactorThrusterBonus";
	private const string CondRoomIntakeBonus = "StatRoomReactorIntakeBonus";

	public static void ApplyBonuses(Room room)
	{
		bool hasThruster = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsRCSClusterInstalled"), "Power");
		bool hasIntake = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsRCSDistroInstalled"), "Power");

		room.CO.SetCondAmount(CondRoomThrusterBonus, hasThruster ? Plugin.ReactorThrusterBonus.Value : 0.0, 0.0);
		room.CO.SetCondAmount(CondRoomIntakeBonus, hasIntake ? Plugin.ReactorIntakeEfficiencyBonus.Value : 0.0, 0.0);
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

			bonus += RoomEffectUtils.GetRoomCondAmount(room, CondRoomThrusterBonus);
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

			bonus += RoomEffectUtils.GetRoomCondAmount(room, CondRoomIntakeBonus);
		}

		return bonus;
	}
}
