namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusRecreation
{
	private const string CondRoomRecreationPositiveBonus = "StatRoomRecreationPositiveBonus";
	private const string CondRoomRecreationNegativeReduction = "StatRoomRecreationNegativeReduction";

	public static void ApplyBonuses(Room room)
	{
		room.CO.SetCondAmount(CondRoomRecreationPositiveBonus, Plugin.RecreationPositiveBonus.Value, 0.0);
		room.CO.SetCondAmount(CondRoomRecreationNegativeReduction, Plugin.RecreationNegativeReduction.Value, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{
		if (interaction == null || coUs == null || interaction.strActionGroup == "Work" || interaction.strActionGroup == "Fight" || interaction.strActionGroup == "Ship")
		{
			return amount;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs);
		if (RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationPositiveBonus) <= 0.0 &&
			RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationNegativeReduction) <= 0.0)
		{
			return amount;
		}

		return RoomEffectUtils.ApplyRecreationTriggerModifier(coUs, amount);
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		if (interaction == null || coUs == null || interaction.strActionGroup == "Work" || interaction.strActionGroup == "Fight" || interaction.strActionGroup == "Ship")
		{
			return amount;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs);
		if (RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationPositiveBonus) <= 0.0 &&
			RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationNegativeReduction) <= 0.0)
		{
			return amount;
		}

		return RoomEffectUtils.ApplyRecreationCondModifier(coUs, amount);
	}
}
