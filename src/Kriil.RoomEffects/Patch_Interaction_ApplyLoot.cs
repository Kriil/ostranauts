using System.Collections.Generic;
using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[HarmonyPatch(typeof(Interaction), "ApplyLootCT")]
internal static class Patch_Interaction_ApplyLootCT
{
	public static bool Prefix(Interaction __instance, Loot LootCTs, Relationship relUs, CondOwner coUs, CondOwner coThem, float fCoeff)
	{
		if (coUs == null)
		{
			return false;
		}

		Dictionary<string, double> applied = null;
		if (LootCTs != null && LootCTs.strName != "Blank")
		{
			List<CondTrigger> ctLoot = LootCTs.GetCTLoot(null, null);
			if (relUs != null)
			{
				applied = new Dictionary<string, double>();
			}

			foreach (CondTrigger condTrigger in ctLoot)
			{
				if (condTrigger == null || !condTrigger.Triggered(coUs, null, true))
				{
					continue;
				}

				float baseAmount = condTrigger.fCount * fCoeff;
				RoomEffectUtils.LogRoomEffect($"Base trigger amount for interaction '{__instance.strName}' and trigger '{condTrigger.strCondName}' between '{coUs.strName}' and '{coThem.strName}': {baseAmount}.", "ApplyLootCT", null);
				float modifiedAmount = RoomEffectUtils.ModifyInteractionTriggerAmount(__instance, condTrigger, coUs, coThem, baseAmount);
				RoomEffectUtils.LogRoomEffect($"Modified trigger amount for interaction '{__instance.strName}' and trigger '{condTrigger.strCondName}' between '{coUs.strName}' and '{coThem.strName}': {modifiedAmount}.", "ApplyLootCT", null);
				float triggerCoeff = fCoeff;
				if (condTrigger.fCount != 0f)
				{
					RoomEffectUtils.LogRoomEffect($"Trigger coefficient for interaction '{__instance.strName}' and trigger '{condTrigger.strCondName}' between '{coUs.strName}' and '{coThem.strName}': {fCoeff} -> {triggerCoeff}.", "ApplyLootCT", null);
					triggerCoeff = modifiedAmount / condTrigger.fCount;
				}

				condTrigger.ApplyChanceID(true, coUs, triggerCoeff, 0f);
				coUs.AddRememberScore(condTrigger.strCondName, modifiedAmount);

				if (applied == null)
				{
					continue;
				}

				if (!applied.ContainsKey(condTrigger.strCondName))
				{
					applied[condTrigger.strCondName] = modifiedAmount;
				}
				else
				{
					applied[condTrigger.strCondName] += modifiedAmount;
				}
			}
		}

		if (coUs == __instance.objUs && !__instance.bHumanOnly)
		{
			coUs.aRememberIAs.Insert(0, __instance.strName);
		}

		coUs.RememberEffects2(coThem);
		if (coUs == __instance.objUs)
		{
			coUs.RememberLess();
		}

		if (relUs != null)
		{
			relUs.StoreCond(coUs, applied, coThem);
		}

		return false;
	}
}

[HarmonyPatch(typeof(Interaction), "ApplyLootConds")]
internal static class Patch_Interaction_ApplyLootConds
{
	public static bool Prefix(Interaction __instance, Loot LootConds, Relationship relUs, CondOwner coUs, CondOwner coThem, float fCoeff)
	{
		if (coUs == null)
		{
			return false;
		}

		Dictionary<string, double> applied = null;
		if (LootConds != null && LootConds.strName != "Blank")
		{
			Dictionary<string, double> condLoot = LootConds.GetCondLoot(1f, null, null);
			if (relUs != null)
			{
				applied = new Dictionary<string, double>();
			}

			foreach (KeyValuePair<string, double> kvp in condLoot)
			{
				double baseAmount = kvp.Value * fCoeff;
				RoomEffectUtils.LogRoomEffect($"Base condition amount for interaction '{__instance.strName}' and condition '{kvp.Key}' between '{coUs.strName}' and '{coThem.strName}': {baseAmount}.", "ApplyLootCond", null);
				double modifiedAmount = RoomEffectUtils.ModifyInteractionCondAmount(__instance, kvp.Key, coUs, coThem, baseAmount);
				RoomEffectUtils.LogRoomEffect($"Modified condition amount for interaction '{__instance.strName}' and condition '{kvp.Key}' between '{coUs.strName}' and '{coThem.strName}': {modifiedAmount}.", "ApplyLootCond", null);
				coUs.AddCondAmount(kvp.Key, modifiedAmount, 0.0, 0f);
				coUs.AddRememberScore(kvp.Key, modifiedAmount);

				if (applied == null)
				{
					continue;
				}

				if (!applied.ContainsKey(kvp.Key))
				{
					applied[kvp.Key] = modifiedAmount;
				}
				else
				{
					applied[kvp.Key] += modifiedAmount;
				}
			}
		}

		if (coUs == __instance.objUs && !__instance.bHumanOnly)
		{
			coUs.aRememberIAs.Insert(0, __instance.strName);
		}

		coUs.RememberEffects2(coThem);
		if (coUs == __instance.objUs)
		{
			coUs.RememberLess();
		}

		if (relUs != null)
		{
			relUs.StoreCond(coUs, applied, coThem);
		}

		return false;
	}
}
