using System.Globalization;
using UnityEngine;

namespace Ostranauts.RoomEffects;

internal static class BonusGalley
{
	private const string GalleyRoomSpec = "Galley";
	private const string CondGalleyFoodRateBuff = "HasHadGalleyFoodRateBuff";
	private const string CondGalleyHydrationRateBuff = "HasHadGalleyHydrationRateBuff";
	private const string LootGalleyFoodRatePer = "CONDGalleyFoodRatePer";
	private const string LootGalleyHydrationRatePer = "CONDGalleyHydrationRatePer";

	public static void ApplyBonuses(Room room)
	{
		UpdateDynamicBuffDefinitions();
		RoomEffectUtils.LogRoomEffect("Galley bonus definitions refreshed.", "Galley", room);
	}

	public static float ModifyTriggerAmount(Interaction interaction, CondTrigger trigger, CondOwner coUs, CondOwner coThem, float amount)
	{
		if (interaction?.strName == null || trigger == null || coUs == null)
		{
			return amount;
		}

		Room room = RoomEffectUtils.GetCondOwnerRoom(coUs);
		if (!RoomEffectUtils.IsRoomSpec(room, GalleyRoomSpec))
		{
			return amount;
		}

		UpdateDynamicBuffDefinitions();

		if (interaction.strName == "SeekFoodAllowDirect" && IsTrencherItem(coThem) && trigger.strCondName == "StatFood" && amount < 0f)
		{
			ApplyPreparedMealBonusSideEffects(coUs);
			RoomEffectUtils.LogRoomEffect("Applying hot-meal equivalent bonuses to Trencher food in Galley.", "Galley", room);
			return amount * (7f / 5f);
		}

		if (interaction.strName.StartsWith("SeekFoodAllowDirect") && trigger.strCondName == "StatFood" && amount < 0f)
		{
			ApplyGalleyFoodRateBuff(coUs, room);
		}

		if (interaction.strName == "SeekDrinkAllowWater" && trigger.strCondName == "StatHydration" && amount < 0f)
		{
			ApplyGalleyHydrationRateBuff(coUs, room);
		}

		return amount;
	}

	public static double ModifyCondAmount(Interaction interaction, string condName, CondOwner coUs, double amount)
	{
		return amount;
	}

	private static void ApplyPreparedMealBonusSideEffects(CondOwner eater)
	{
		eater.AddCondAmount("StatMeaning", -18.0, 0.0, 0f);
		eater.AddCondAmount("StatSelfRespect", -9.0, 0.0, 0f);
		eater.AddCondAmount("StatAltruism", 6.0, 0.0, 0f);
		eater.AddCondAmount("HasHadTastyMeal", 1.0, 0.0, 0f);
	}

	private static void ApplyGalleyFoodRateBuff(CondOwner eater, Room room)
	{
		eater.AddCondAmount(CondGalleyFoodRateBuff, 1.0, 0.0, 0f);
		RoomEffectUtils.LogRoomEffect("Applied temporary galley food-rate bonus.", "Galley", room);
	}

	private static void ApplyGalleyHydrationRateBuff(CondOwner eater, Room room)
	{
		eater.AddCondAmount(CondGalleyHydrationRateBuff, 1.0, 0.0, 0f);
		RoomEffectUtils.LogRoomEffect("Applied temporary galley hydration-rate bonus.", "Galley", room);
	}

	private static bool IsTrencherItem(CondOwner item)
	{
		return item?.strName != null && item.strName.StartsWith("ItmTrencher");
	}

	private static void UpdateDynamicBuffDefinitions()
	{
		SetDynamicPerLoot(LootGalleyFoodRatePer, "StatFoodRate", Mathf.Max(0f, Plugin.GalleyFoodRateReduction.Value));
		SetDynamicPerLoot(LootGalleyHydrationRatePer, "StatHydrationRate", Mathf.Max(0f, Plugin.GalleyHydrationRateReduction.Value));
		SetDynamicDuration(CondGalleyFoodRateBuff, Mathf.Max(0f, Plugin.GalleyFoodRateDurationHours.Value));
		SetDynamicDuration(CondGalleyHydrationRateBuff, Mathf.Max(0f, Plugin.GalleyHydrationRateDurationHours.Value));
	}

	private static void SetDynamicPerLoot(string lootName, string statName, float reduction)
	{
		Loot loot = DataHandler.GetLoot(lootName);
		if (loot == null)
		{
			return;
		}

		string amount = reduction.ToString("0.####", CultureInfo.InvariantCulture);
		loot.aCOs = new[] { $"-{statName}=1.0x{amount}" };
	}

	private static void SetDynamicDuration(string condName, float durationHours)
	{
		if (DataHandler.dictConds.TryGetValue(condName, out JsonCond condDef) && condDef != null)
		{
			condDef.fDuration = durationHours;
		}
	}
}