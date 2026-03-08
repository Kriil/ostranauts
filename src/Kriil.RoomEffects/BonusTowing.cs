namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusTowing
{
	private const string CondRoomTowingSpeedBonus = "StatRoomTowingSecureSpeedBonus";

	public static void ApplyBonuses(Room room)
	{	
		float towBraceBonus = 0f;
		bool hasTowBrace = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsTowingBraceInstalled"), "Power");
		if (hasTowBrace)
		{
			towBraceBonus = Plugin.TowingSecureSpeedBonus.Value;
			RoomEffectUtils.LogRoomEffect($"Setting Towing Secure Speed Bonus of {towBraceBonus * 100f}% due to towing brace device installed in room.", "Towing", room);
		} else {
			RoomEffectUtils.LogRoomEffect($"No towing brace device installed in room, setting bonus to 0%.", "Towing", room);
		}
		room.CO.SetCondAmount(CondRoomTowingSpeedBonus, towBraceBonus, 0.0);
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
		RoomEffectUtils.LogRoomEffect($"Applying towing speed bonus of {bonus * 100f}% to interaction '{interaction.strName}' with base duration of {durationHours} hours.", "Towing", targetRoom);
		return RoomEffectUtils.ApplySpeedBonus(durationHours, bonus);
	}
}
