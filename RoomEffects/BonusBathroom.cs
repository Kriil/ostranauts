namespace Ostranauts.RoomEffects;

internal static class BonusBathroom
{
	private const string RoomSpecName = "Bathroom";
	private const string CondRoomBathroomSpeedBonus = "StatRoomBathroomSpeedBonus";

	public static void ApplyBonuses(Room room)
	{
		room.CO.SetCondAmount(CondRoomBathroomSpeedBonus, Plugin.BathroomSpeedBonus.Value, 0.0);
	}

	public static float ModifyInteractionDuration(Interaction interaction, float durationHours)
	{
		if (!RoomEffectUtils.IsPlayerInteraction(interaction))
		{
			return durationHours;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(interaction.objUs);
		if (!RoomEffectUtils.IsRoomSpec(room, RoomSpecName))
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

		
		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(room, CondRoomBathroomSpeedBonus);
		RoomEffectUtils.LogRoomEffect($"Applied bathroom speed bonus of {bonus * 100f}% to interaction '{interaction.strName}' with base duration of {durationHours} hours.", "Bathroom", room);
		return RoomEffectUtils.ApplySpeedBonus(durationHours, bonus);
	}
}
