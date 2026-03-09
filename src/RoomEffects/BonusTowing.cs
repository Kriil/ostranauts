namespace Ostranauts.RoomEffects;

internal static class BonusTowing
{
	private const string RoomSpecName = "TowingRoom";
	private const string CondRoomTowingSpeedBonus = "StatRoomTowingSecureSpeedBonus";

	public static void ApplyBonuses(Room room)
	{	
		float towBraceBonus = 0f;
		bool hasTowBrace = RoomEffectUtils.HasInstalledDeviceInRoomByPoint(room, DataHandler.GetCondTrigger("TIsTowingBraceInstalled"), "Power");
		if (hasTowBrace)
		{
			towBraceBonus = Plugin.TowingSecureSpeedBonus.Value;
		} 
		room.CO.SetCondAmount(CondRoomTowingSpeedBonus, towBraceBonus, 0.0);
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
		if (name == null || !name.StartsWith("ACTTowingSecure"))
		{
			return durationHours;
		}

		Room targetRoom = RoomEffectUtils.GetCondOwnerRoom(interaction.objThem, "Power");
		double bonus = RoomEffectUtils.GetRoomCondAmount(targetRoom, CondRoomTowingSpeedBonus);
		RoomEffectUtils.LogRoomEffect($"Applied towing speed bonus of {bonus * 100f}% to interaction '{interaction.strName}' with base duration of {durationHours} hours.", "Towing", targetRoom);
		return RoomEffectUtils.ApplySpeedBonus(durationHours, bonus);
	}
}
