namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusAirlock
{
	private const string CondRoomScrubberSpeedBonus = "StatRoomAirlockScrubberSpeedBonus";

	public static void ApplyBonuses(Room room)
	{
		bool hasScrubber =
			RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsAtmoScrubber01Installed"), "GasInput") ||
			RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsAtmoScrubber02Installed"), "GasInput");

		if (hasScrubber)
		{
			RoomEffectUtils.LogRoomEffect($"Applied Airlock Scrubber Speed Bonus of {Plugin.AirlockScrubberSpeedBonus.Value * 100f}% due to scrubber device installed in room.", "Airlock", room);
		} else {
			RoomEffectUtils.LogRoomEffect($"No scrubber device installed in room, setting bonus to 0%.", "Airlock", room);
		}
		room.CO.SetCondAmount(CondRoomScrubberSpeedBonus, hasScrubber ? Plugin.AirlockScrubberSpeedBonus.Value : 0.0, 0.0);
	}

	public static double GetScrubberSpeedBonus(Room room)
	{
		return RoomEffectUtils.GetRoomCondAmount(room, CondRoomScrubberSpeedBonus);
	}
}
