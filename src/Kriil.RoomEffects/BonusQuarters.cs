namespace Kriil.Ostranauts.RoomEffects;

// TODO: Game doesn't seem to distinguish between basic and luxury quarters like it supposed to.  Game bug? Ship rater ias not showing luxury quarters even though conditions are met.
internal static class BonusQuarters
{
	private const string CondRoomSleepBonus = "StatRoomSleepEfficiencyBonus";

	public static void ApplyBonuses(Room room, bool isLuxury)
	{
		float bonus = isLuxury ? Plugin.LuxurySleepBonus.Value : Plugin.BasicSleepBonus.Value;
		RoomEffectUtils.LogRoomEffect($"Setting {(isLuxury ? "Luxury" : "Basic")} Sleep Bonus of {bonus * 100f}%", isLuxury ? "LuxuryQuarters" : "BasicQuarters", room);
		room.CO.SetCondAmount(CondRoomSleepBonus, bonus, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{
		if (!IsSleepTickInteraction(interaction) || coUs == null || trigger == null)
		{
			return amount;
		}

		double bonus = GetSleepRoomBonus(interaction, coUs);
		RoomEffectUtils.LogRoomEffect($"Calculated sleep tick trigger for interaction '{interaction.strName}' and trigger '{trigger.strCondName}' with bonus of {bonus * 100.0}%.", "Quarters", RoomEffectUtils.GetCondOwnerRoom(coUs));
		if (bonus <= 0.0)
		{
			return amount;
		}

		if (trigger.strCondName == "FFWDRefreshedBuildUp" ||
			trigger.strCondName == "FFWDRefreshedBuildUp2" ||
			trigger.strCondName == "FFWDRefreshedBuildUp3" ||
			trigger.strCondName == "FFWDRefreshedBuildUp4" ||
			trigger.strCondName == "FFWDRefreshed" ||
			trigger.strCondName == "FFWDRefreshed2" ||
			trigger.strCondName == "FFWDRefreshed3" ||
			trigger.strCondName == "FFWDRefreshed4")
		{
			RoomEffectUtils.LogRoomEffect($"Applying sleep tick trigger '{trigger.strCondName}' with base amount {amount}, using bonus {bonus * 100.0}%.", "Quarters", RoomEffectUtils.GetCondOwnerRoom(coUs));
			return amount * (1f + (float)bonus);
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		if (!IsSleepTickInteraction(interaction) || coUs == null)
		{
			return amount;
		}

		double bonus = GetSleepRoomBonus(interaction, coUs);
		
		if (bonus <= 0.0)
		{
			return amount;
		}

		if (condName == "StatSleep")
		{
			RoomEffectUtils.LogRoomEffect($"Applying sleep tick condition '{condName}' with base amount {amount}, using bonus {bonus * 100.0}%.", "Quarters", RoomEffectUtils.GetCondOwnerRoom(coUs));
			return amount * (1.0 + bonus);
		}

		return amount;
	}

	private static bool IsSleepTickInteraction(Interaction interaction)
	{
		return interaction?.strName != null && interaction.strName.StartsWith("Tick1HourSleep");
	}

	private static double GetSleepRoomBonus(Interaction interaction, CondOwner sleeper)
	{
		Room sleepRoom = ResolveSleepRoom(interaction, sleeper);
		double bonus = RoomEffectUtils.GetRoomCondAmount(sleepRoom, CondRoomSleepBonus);
		return bonus;
	}

	private static Room ResolveSleepRoom(Interaction tickInteraction, CondOwner sleeper)
	{
		Room room = GetRoomFromSleepInteraction(tickInteraction, sleeper);
		if (room != null)
		{
			return room;
		}

		Interaction current = sleeper.GetInteractionCurrent();
		room = GetRoomFromSleepInteraction(current, sleeper);
		if (room != null)
		{
			return room;
		}

		if (sleeper.aQueue != null)
		{
			for (int i = 0; i < sleeper.aQueue.Count; i++)
			{
				room = GetRoomFromSleepInteraction(sleeper.aQueue[i], sleeper);
				if (room != null)
				{
					return room;
				}
			}
		}

		return RoomEffectUtils.GetCondOwnerRoom(sleeper);
	}

	private static Room GetRoomFromSleepInteraction(Interaction interaction, CondOwner sleeper)
	{
		if (interaction?.objThem == null || interaction.objThem == sleeper)
		{
			return null;
		}

		if (!IsSleepTickInteraction(interaction) && (interaction.strName == null || !interaction.strName.StartsWith("SeekSleepSimple")))
		{
			return null;
		}

		CondOwner target = interaction.objThem;
		if (!target.HasCond("IsBed") && !target.HasCond("IsBedMedical") && !target.HasCond("IsBedComfortable"))
		{
			return null;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(target, "use");
		if (room == null)
		{
			room = RoomEffectUtils.GetCondOwnerRoom(target);
		}

		return room;
	}
}
