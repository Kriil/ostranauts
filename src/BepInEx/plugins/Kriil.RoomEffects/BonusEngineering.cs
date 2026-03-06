namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusEngineering
{
	public static void ApplyBonuses(Room room)
	{
		double heatBonus = 0.0;
		double coolBonus = 0.0;

		CondTrigger heaterTrigger = DataHandler.GetCondTrigger("TIsHeater01Installed");
		CondTrigger coolerTrigger = DataHandler.GetCondTrigger("TIsCooler01Installed");

		if (RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, heaterTrigger, "use"))
		{
			heatBonus = Plugin.EngineeringHeatBonus.Value;
		}
		if (RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, coolerTrigger, "use"))
		{
			coolBonus = Plugin.EngineeringCoolBonus.Value;
		}


		room.CO.SetCondAmount("StatRoomHeatSpeedBonus", heatBonus, 0.0);
		room.CO.SetCondAmount("StatRoomCoolSpeedBonus", coolBonus, 0.0);
	}

	public static void ApplyShipBonuses(Ship ship, CondOwner shipCo)
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
}