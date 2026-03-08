using UnityEngine;

namespace Kriil.Ostranauts.RoomEffects;

internal static class BonusRecreation
{
	private const string CondRoomRecreationPositiveBonus = "StatRoomRecreationPositiveBonus";
	private const string CondRoomRecreationNegativeReduction = "StatRoomRecreationNegativeReduction";

	public static void ApplyBonuses(Room room)
	{
		float positiveBonus = Plugin.RecreationPositiveBonus.Value;
		float negativeReduction = Plugin.RecreationNegativeReduction.Value;
		RoomEffectUtils.LogRoomEffect($"Setting Recreation Positive Bonus of {positiveBonus * 100f}% and Negative Reduction of {negativeReduction * 100f}%.", "Recreation", room);
		room.CO.SetCondAmount(CondRoomRecreationPositiveBonus, positiveBonus, 0.0);
		room.CO.SetCondAmount(CondRoomRecreationNegativeReduction, negativeReduction, 0.0);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, float amount)
	{
		if (interaction == null || coUs == null || interaction.strActionGroup == "Work" || interaction.strActionGroup == "Fight" || interaction.strActionGroup == "Ship")
		{
			return amount;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs);
		double positiveBonus = RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationPositiveBonus);
		double negativeReduction = RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationNegativeReduction);

		if (positiveBonus <= 0.0 && negativeReduction <= 0.0)
		{
			return amount;
		}

		if (amount < 0f && positiveBonus > 0.0)
		{
			float bonusAmount = amount * (1f + (float)positiveBonus);
			RoomEffectUtils.LogRoomEffect($"Modified trigger amount after applying {positiveBonus * 100f}% recreation positive bonus: {bonusAmount}.", "Recreation", room);
			return bonusAmount;
		}

		if (amount > 0f && negativeReduction > 0.0)
		{
			float bonusAmount = amount * Mathf.Max(0f, 1f - (float)negativeReduction);
			RoomEffectUtils.LogRoomEffect($"Modified trigger amount after applying {negativeReduction * 100f}% recreation negative reduction: {bonusAmount}.", "Recreation", room);
			return bonusAmount;
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		if (interaction == null || coUs == null || interaction.strActionGroup == "Work" || interaction.strActionGroup == "Fight" || interaction.strActionGroup == "Ship")
		{
			return amount;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs);
		double positiveBonus = RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationPositiveBonus);
		double negativeReduction = RoomEffectUtils.GetRoomCondAmount(room, CondRoomRecreationNegativeReduction);
		if (positiveBonus <= 0.0 && negativeReduction <= 0.0)
		{
			return amount;
		}

		if (amount < 0.0 && positiveBonus > 0.0)
		{
			float bonusAmount = (float)(amount * (1.0 + positiveBonus));
			RoomEffectUtils.LogRoomEffect($"Modified condition amount after applying {positiveBonus * 100f}% recreation positive bonus: {bonusAmount}.", "Recreation", room);
			return bonusAmount;
		}

		if (amount > 0.0 && negativeReduction > 0.0)
		{
			float bonusAmount = (float)(amount * Mathf.Max(0f, 1f - (float)negativeReduction));
			RoomEffectUtils.LogRoomEffect($"Modified condition amount after applying {negativeReduction * 100f}% recreation negative reduction: {bonusAmount}.", "Recreation", room);
			return bonusAmount;
		}

		return amount;
	}
}
