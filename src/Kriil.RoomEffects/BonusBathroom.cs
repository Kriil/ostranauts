namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusBathroom
{
	private const string CondRoomBathroomSpeedBonus = "StatRoomBathroomSpeedBonus";

	public static void ApplyBonuses(Room room)
	{
		float bonus = Plugin.BathroomSpeedBonus.Value;
		RoomEffectUtils.LogRoomEffect($"Setting Bathroom Speed Bonus of {bonus * 100f}%.", "Bathroom", room);
		room.CO.SetCondAmount(CondRoomBathroomSpeedBonus, bonus, 0.0);
	}

	public static float ModifyInteractionDuration(Interaction interaction, float durationHours)
	{
		if (!RoomEffectUtils.IsPlayerInteraction(interaction))
		{
			return durationHours;
		}

		string name = interaction?.strName;
		if (name == null ||
			(!name.StartsWith("SeekDefecation") &&
			name != "Defecation" &&
			name != "DefecationFinish" &&
			!name.StartsWith("SeekHygiene") &&
			!name.StartsWith("ACTSeekHygiene")))
		{
			return durationHours;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(interaction.objUs);
		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(room, CondRoomBathroomSpeedBonus);
		RoomEffectUtils.LogRoomEffect($"Applying bathroom speed bonus of {bonus * 100f}% to interaction '{interaction.strName}' with base duration of {durationHours} hours.", "Bathroom", room);
		return RoomEffectUtils.ApplySpeedBonus(durationHours, bonus);
	}
}
