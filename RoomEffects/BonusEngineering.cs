namespace Ostranauts.RoomEffects;

internal static class BonusEngineering
{
	private const string roomSpecName = "Engineering";
	public static void ApplyBonuses(Room room)
	{
		// Placeholder for future engineering bonuses, currently no ship or room bonuses to apply
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

		float bonus = 0f;
		if (hasEngineering)
		{
			bonus = Plugin.EngineeringWorkBonus.Value;
		} 
		shipCo.SetCondAmount("StatShipEngineeringWorkBonus", bonus, 0.0);
	}
}