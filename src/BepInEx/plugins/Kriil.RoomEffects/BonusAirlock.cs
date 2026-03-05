namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusAirlock
{
	private const string CondRoomScrubberSpeedBonus = "StatRoomAirlockScrubberSpeedBonus";

	public static void ApplyBonuses(Room room)
	{
		bool hasScrubber =
			RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsAtmoScrubber01Installed"), "GasInput") ||
			RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsAtmoScrubber02Installed"), "GasInput");

		room.CO.SetCondAmount(CondRoomScrubberSpeedBonus, hasScrubber ? Plugin.AirlockScrubberSpeedBonus.Value : 0.0, 0.0);
	}

	public static double GetScrubberSpeedBonus(Room room)
	{
		return RoomEffectUtils.GetRoomCondAmount(room, CondRoomScrubberSpeedBonus);
	}
}
