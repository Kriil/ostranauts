namespace Ostranauts.RoomEffects;

internal static class BonusWellness
{
	private const string RoomSpecName = "WellnessRoom";
	private const string CondRoomWellnessFitnessBonus = "StatRoomWellnessFitnessBonus";
	private const string CondRoomWellnessStrengthBonus = "StatRoomWellnessStrengthBonus";

	public static void ApplyBonuses(Room room)
	{
		room.CO.SetCondAmount(CondRoomWellnessFitnessBonus, Plugin.WellnessFitnessBonus.Value, 0.0);
		room.CO.SetCondAmount(CondRoomWellnessStrengthBonus, Plugin.WellnessStrengthBonus.Value, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs, "Power");
		if (!RoomEffectUtils.IsRoomSpec(room, RoomSpecName))
		{
			return amount;
		}

		if (interaction?.strName == "Tick1HourExerciseTreadmill" || interaction?.strName == "ACTExcerciseTreadmillDo")
		{
			double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(room, CondRoomWellnessFitnessBonus);
			if (bonus > 0.0 &&
				(trigger.strCondName == "StatTrainingFit" || trigger.strCondName == "StatTrainingUnfit"))
			{
				RoomEffectUtils.LogRoomEffect($"Applied trigger amount for '{trigger.strCondName}' with wellness fitness bonus of {bonus * 100f}%.", "Wellness", room);
				return amount * (1f + (float)bonus);
			}
		}

		if (interaction?.strName == "Tick1HourExerciseStrengthTrainer" || interaction?.strName == "ACTExcerciseStrengthTrainerDo")
		{
			double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(room, CondRoomWellnessStrengthBonus);
			if (bonus > 0.0 &&
				(trigger.strCondName == "StatTrainingStrong" ||	trigger.strCondName == "StatTrainingFeeble"))
			{
				RoomEffectUtils.LogRoomEffect($"Applied trigger amount for '{trigger.strCondName}' with wellness strength bonus of {bonus * 100f}%.", "Wellness", room);
				return amount * (1f + (float)bonus);
			}
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		// Wellness currently modifies CT-driven training deltas, not direct condition loot values.
		return amount;
	}
}
