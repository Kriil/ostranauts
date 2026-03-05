namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusGalley
{
	private const string CondRoomGalleySatietyBonus = "StatRoomGalleySatietyBonus";

	public static void ApplyBonuses(Room room)
	{
		room.CO.SetCondAmount(CondRoomGalleySatietyBonus, Plugin.GalleySatiationBonus.Value, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{
		if (interaction?.strName == null || coUs == null || !interaction.strName.StartsWith("SeekFood"))
		{
			return amount;
		}

		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(coUs, CondRoomGalleySatietyBonus);
		if (bonus <= 0.0)
		{
			return amount;
		}

		if (trigger.strCondName == "StatSatiety" || trigger.strCondName == "StatFood")
		{
			return amount * (1f + (float)bonus);
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		if (interaction?.strName == null || coUs == null || !interaction.strName.StartsWith("SeekFood"))
		{
			return amount;
		}

		double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(coUs, CondRoomGalleySatietyBonus);
		if (bonus <= 0.0)
		{
			return amount;
		}

		if ((condName == "StatSatiety" || condName == "StatFood") && amount > 0.0)
		{
			return amount * (1.0 + bonus);
		}

		return amount;
	}
}
