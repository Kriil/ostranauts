namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusGalley
{
	private const string CondRoomGalleySatietyBonus = "StatRoomGalleySatietyBonus";

	public static void ApplyBonuses(Room room)
	{
		float bonus = Plugin.GalleySatiationBonus.Value;
		RoomEffectUtils.LogRoomEffect($"Setting Galley Satiety Bonus of {bonus * 100f}%.", "Galley", room);
		room.CO.SetCondAmount(CondRoomGalleySatietyBonus, bonus, 0.0);
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

		// TODO: Eating any amount seems to fully statiate the character so maybe check if their are other stats that can be affected instead
		if (trigger.strCondName == "StatSatiety" || trigger.strCondName == "StatFood")
		{
			RoomEffectUtils.LogRoomEffect($"Applying galley satiety trigger '{trigger.strCondName}' with base amount {amount}, using bonus {bonus * 100.0}%.", "Galley", RoomEffectUtils.GetCondOwnerRoom(coUs));
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

		// TODO: Eating any amount seems to fully statiate the character so maybe check if their are other stats that can be affected instead
		if ((condName == "StatSatiety" || condName == "StatFood") && amount > 0.0)
		{
			RoomEffectUtils.LogRoomEffect($"Applying galley satiety condition '{condName}' with base amount {amount}, using bonus {bonus * 100.0}%.", "Galley", RoomEffectUtils.GetCondOwnerRoom(coUs));
			return amount * (1.0 + bonus);
		}

		return amount;
	}
}
