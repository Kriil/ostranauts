namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusBathroom
{
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

		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(interaction.objUs, CondRoomBathroomSpeedBonus);
		return RoomEffectUtils.ApplySpeedBonus(durationHours, bonus);
	}
}
