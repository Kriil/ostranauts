using HarmonyLib;

namespace Ostranauts.ConstructionTweaks;

[HarmonyPatch(typeof(Tile), "get_IsWall")]
public static class Patch_PlaceholderWallFlag
{
	[HarmonyPostfix]
	private static void GetIsWallPostfix(Tile __instance, ref bool __result)
	{
		try
		{
			if (!__result || __instance == null)
			{
				return;
			}

			if (PlaceholderTraversalHelper.IsPlaceholderOnlyBlockedTile(__instance))
			{
				__result = false;
			}
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException("Patch_PlaceholderWallFlag.GetIsWallPostfix", ex);
		}
	}
}

[HarmonyPatch(typeof(Tile), nameof(Tile.IsWalkable))]
public static class Patch_PlaceholderWalkability
{
	[HarmonyPostfix]
	private static void IsWalkablePostfix(Tile __instance, CondOwner coUs, PathResult pr, ref bool __result)
	{
		try
		{
			if (__result || __instance == null || coUs == null || coUs.ship == null)
			{
				return;
			}

			if (!PlaceholderTraversalHelper.IsCrew(coUs))
			{
				return;
			}

			if (__instance.IsForbidden(coUs) || __instance.IsEvaTileWithGravitation())
			{
				return;
			}

			if (!PlaceholderTraversalHelper.IsPlaceholderOnlyBlockedTile(__instance))
			{
				return;
			}

			__result = true;
			if (pr != null)
			{
				pr.bAirlockBlocked = false;
				pr.bGravBlocked = false;
				pr.bForbidZoneBlocked = false;
			}
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException("Patch_PlaceholderWalkability.IsWalkablePostfix", ex);
		}
	}
}
