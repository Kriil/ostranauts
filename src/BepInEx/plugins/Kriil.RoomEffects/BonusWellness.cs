namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusWellness
{
	private const string CondRoomWellnessFitnessBonus = "StatRoomWellnessFitnessBonus";
	private const string CondRoomWellnessStrengthBonus = "StatRoomWellnessStrengthBonus";

	public static void ApplyBonuses(Room room)
	{
		room.CO.SetCondAmount(CondRoomWellnessFitnessBonus, Plugin.WellnessFitnessBonus.Value, 0.0);
		room.CO.SetCondAmount(CondRoomWellnessStrengthBonus, Plugin.WellnessStrengthBonus.Value, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{
		if (interaction?.strName == "Tick1HourExerciseTreadmill")
		{
			double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(coUs, CondRoomWellnessFitnessBonus);
			if (bonus > 0.0 &&
				(trigger.strCondName == "StatAtrophy" || trigger.strCondName == "StatTrainingFit" || trigger.strCondName == "StatTrainingUnfit"))
			{
				return amount * (1f + (float)bonus);
			}
		}

		if (interaction?.strName == "Tick1HourExerciseStrengthTrainer")
		{
			double bonus = RoomEffectUtils.GetCondOwnerRoomBonus(coUs, CondRoomWellnessStrengthBonus);
			if (bonus > 0.0 &&
				(trigger.strCondName == "StatAtrophy" ||
				trigger.strCondName == "StatTrainingStrong" ||
				trigger.strCondName == "StatTrainingStrongAtrophy" ||
				trigger.strCondName == "StatTrainingFeeble"))
			{
				return amount * (1f + (float)bonus);
			}
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		return amount;
	}
}
