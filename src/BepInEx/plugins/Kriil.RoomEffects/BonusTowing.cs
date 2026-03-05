namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusTowing
{
	private const string CondRoomTowingSpeedBonus = "StatRoomTowingSecureSpeedBonus";

	public static void ApplyBonuses(Room room)
	{
		bool hasTowBrace = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsTowingBraceInstalled"), "Power");
		room.CO.SetCondAmount(CondRoomTowingSpeedBonus, hasTowBrace ? Plugin.TowingSecureSpeedBonus.Value : 0.0, 0.0);
	}

	public static float ModifyInteractionDuration(Interaction interaction, float durationHours)
	{
		if (!RoomEffectUtils.IsPlayerInteraction(interaction))
		{
			return durationHours;
		}

		string name = interaction?.strName;
		if (name == null || !name.StartsWith("ACTTowingSecure"))
		{
			return durationHours;
		}

		Room targetRoom = RoomEffectUtils.GetCondOwnerRoom(interaction.objThem, "Power");
		double bonus = RoomEffectUtils.GetRoomCondAmount(targetRoom, CondRoomTowingSpeedBonus);
		return RoomEffectUtils.ApplySpeedBonus(durationHours, bonus);
	}
}
