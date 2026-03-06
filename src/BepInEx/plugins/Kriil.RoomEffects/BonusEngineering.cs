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
			RoomEffectUtils.LogRoomEffect($"Applied Engineering Heat Bonus of {heatBonus * 100f}% due to heater device.", "Engineering", room);
		} else {
			RoomEffectUtils.LogRoomEffect($"No heater device installed in room, setting bonus to 0%.", "Engineering", room);
		}
		if (RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, coolerTrigger, "use"))
		{
			coolBonus = Plugin.EngineeringCoolBonus.Value;
			RoomEffectUtils.LogRoomEffect($"Applied Engineering Cool Bonus of {coolBonus * 100f}% due to cooler device.", "Engineering", room);
		} else {
			RoomEffectUtils.LogRoomEffect($"No cooler device installed in room, setting bonus to 0%.", "Engineering", room);
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

		if (hasEngineering)
		{
			RoomEffectUtils.LogRoomEffect($"Applying ship-wide engineering work bonus of {Plugin.EngineeringWorkBonus.Value * 100f}% due to presence of engineering room on ship.", "Engineering", null);
		} else {
			RoomEffectUtils.LogRoomEffect($"No engineering room found on ship, setting ship-wide engineering work bonus to 0%.", "Engineering", null);
		}
		shipCo.SetCondAmount("StatShipEngineeringWorkBonus", hasEngineering ? Plugin.EngineeringWorkBonus.Value : 0.0, 0.0);
	}
}