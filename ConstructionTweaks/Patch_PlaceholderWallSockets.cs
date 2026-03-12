using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Ostranauts.ConstructionTweaks;

[HarmonyPatch(typeof(Item), nameof(Item.SetData), typeof(string), typeof(float), typeof(float))]
internal static class Patch_PlaceholderSocketAdds
{
	private static void Postfix(Item __instance)
	{
		if (Plugin.PlaceholderWallSockets == null || !Plugin.PlaceholderWallSockets.Value)
		{
			return;
		}

		if (__instance == null || !__instance.bPlaceholder || __instance.aSocketAdds == null)
		{
			return;
		}

		try
		{
			bool changed = false;
			for (int i = 0; i < __instance.aSocketAdds.Count; i++)
			{
				Loot loot = __instance.aSocketAdds[i];
				if (loot == null)
				{
					continue;
				}

				if (loot.strName == "TILWallAdds")
				{
					__instance.aSocketAdds[i] = DataHandler.GetLoot(Plugin.PlaceholderWallAddLootName);
					changed = true;
					continue;
				}

				if (loot.strName == "TILWallThinAdds")
				{
					__instance.aSocketAdds[i] = DataHandler.GetLoot("Blank");
					changed = true;
				}
			}

			if (changed)
			{
				__instance.nHeightInTiles = __instance.aSocketAdds.Count / __instance.jid.nCols;
			}
		}
		catch (Exception ex)
		{
			Plugin.LogPatchException(nameof(Patch_PlaceholderSocketAdds), ex);
		}
	}
}

[HarmonyPatch(typeof(CondTrigger), nameof(CondTrigger.Triggered), typeof(CondOwner), typeof(string), typeof(bool))]
internal static class Patch_PlaceholderWallReqs
{
	[ThreadStatic]
	private static bool _isHandlingPlaceholderWall;

	private static void Postfix(CondTrigger __instance, CondOwner objOwner, string strIAStatsName, bool logOutcome, ref bool __result)
	{
		if (Plugin.PlaceholderWallSockets == null || !Plugin.PlaceholderWallSockets.Value)
		{
			return;
		}

		if (__result || _isHandlingPlaceholderWall || __instance == null || objOwner == null)
		{
			return;
		}

		if (!objOwner.HasCond(Plugin.PlaceholderWallConditionName))
		{
			return;
		}

		string[] reqs = __instance.aReqs;
		if (reqs == null || reqs.Length == 0 || Array.IndexOf(reqs, "IsWall") < 0)
		{
			return;
		}

		try
		{
			string[] remappedReqs = (string[])reqs.Clone();
			for (int i = 0; i < remappedReqs.Length; i++)
			{
				if (remappedReqs[i] == "IsWall")
				{
					remappedReqs[i] = Plugin.PlaceholderWallConditionName;
				}
			}

			CondTrigger trigger = new CondTrigger
			{
				strName = __instance.strName,
				strCondName = __instance.strCondName,
				fChance = __instance.fChance,
				fCount = __instance.fCount,
				aReqs = remappedReqs,
				aForbids = __instance.aForbids ?? new string[0],
				aTriggers = __instance.aTriggers ?? new string[0],
				aTriggersForbid = __instance.aTriggersForbid ?? new string[0],
				strHigherCond = __instance.strHigherCond,
				aLowerConds = __instance.aLowerConds ?? new string[0],
				bAND = __instance.bAND
			};

			_isHandlingPlaceholderWall = true;
			__result = trigger.Triggered(objOwner, strIAStatsName, logOutcome);
		}
		catch (Exception ex)
		{
			Plugin.LogPatchException(nameof(Patch_PlaceholderWallReqs), ex);
		}
		finally
		{
			_isHandlingPlaceholderWall = false;
		}
	}
}
