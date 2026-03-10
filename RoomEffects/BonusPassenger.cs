namespace Ostranauts.RoomEffects;

internal static class BonusPassenger
{
	private const string RoomSpecName = "Passenger";
	private const string CondRoomPassengerRelaxBonus = "StatRoomPassengerRelaxBonus";

	public static void ApplyBonuses(Room room, bool isSmall)
	{
		room.CO.SetCondAmount(CondRoomPassengerRelaxBonus, isSmall ? Plugin.PassengerSmallRelaxBonus.Value : Plugin.PassengerMediumRelaxBonus.Value, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{
		if (interaction?.strName == null || coUs == null || !interaction.strName.StartsWith("ACTSeekSecurityChair"))
		{
			return amount;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs);	
		if (!RoomEffectUtils.IsRoomSpec(room, RoomSpecName))
		{
			return amount;
		}

		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(room, CondRoomPassengerRelaxBonus);
		if (bonus <= 0.0)
		{
			return amount;
		}
		
		if (trigger.strCondName == "StatSecurity" && amount < 0f)
		{
			RoomEffectUtils.LogRoomEffect($"Applied passenger relax trigger '{trigger.strCondName}' with base amount {amount}, using bonus {bonus * 100.0}%.", "Passenger", room);
			return amount * (1f + (float)bonus);
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		if (interaction?.strName == null || coUs == null || !interaction.strName.StartsWith("ACTSeekSecurityChair"))
		{
			return amount;
		}

		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(coUs, CondRoomPassengerRelaxBonus);
		if (bonus <= 0.0)
		{
			return amount;
		}

		if (condName == "StatSecurity" && amount < 0.0)
		{
			RoomEffectUtils.LogRoomEffect($"Applied passenger relax condition '{condName}' with base amount {amount}, using bonus {bonus * 100.0}%.", "Passenger", RoomEffectUtils.GetCondOwnerRoom(coUs));
			return amount * (1.0 + bonus);
		}

		return amount;
	}
}
