using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Ostranauts.ConstructionTweaks;

[HarmonyPatch(typeof(Ship), "AddCO", new[]
{
	typeof(CondOwner),
	typeof(bool)
})]
public static class Patch_PlaceholderCrewCollision_AddCO2
{
	[HarmonyPostfix]
	private static void Postfix(Ship __instance, CondOwner objICO)
	{
		try
		{
			if (__instance == null || objICO == null)
			{
				return;
			}

			if (!PlaceholderTraversalHelper.IsCrew(objICO) && !PlaceholderTraversalHelper.IsUninstalledPlaceholder(objICO))
			{
				return;
			}

			Patch_PlaceholderCrewCollision_Common.RefreshShipTraversal(__instance);
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException("Patch_PlaceholderCrewCollision_AddCO2.Postfix", ex);
		}
	}
}

[HarmonyPatch(typeof(Ship), "AddCO", new[]
{
	typeof(CondOwner),
	typeof(bool),
	typeof(bool)
})]
public static class Patch_PlaceholderCrewCollision_AddCO3
{
	[HarmonyPostfix]
	private static void Postfix(Ship __instance, CondOwner objICO)
	{
		try
		{
			if (__instance == null || objICO == null)
			{
				return;
			}

			if (!PlaceholderTraversalHelper.IsCrew(objICO) && !PlaceholderTraversalHelper.IsUninstalledPlaceholder(objICO))
			{
				return;
			}

			Patch_PlaceholderCrewCollision_Common.RefreshShipTraversal(__instance);
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException("Patch_PlaceholderCrewCollision_AddCO3.Postfix", ex);
		}
	}
}

[HarmonyPatch(typeof(Ship), nameof(Ship.RemoveCO), new[]
{
	typeof(CondOwner),
	typeof(bool)
})]
public static class Patch_PlaceholderCrewCollision_RemoveCO
{
	[HarmonyPostfix]
	private static void Postfix(Ship __instance, CondOwner objCO)
	{
		try
		{
			if (__instance == null || objCO == null)
			{
				return;
			}

			if (!PlaceholderTraversalHelper.IsCrew(objCO) && !PlaceholderTraversalHelper.IsUninstalledPlaceholder(objCO))
			{
				return;
			}

			Patch_PlaceholderCrewCollision_Common.RefreshShipTraversal(__instance);
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException("Patch_PlaceholderCrewCollision_RemoveCO.Postfix", ex);
		}
	}
}

internal static class Patch_PlaceholderCrewCollision_Common
{
	internal static void RefreshShipTraversal(Ship ship)
	{
		try
		{
			PlaceholderTraversalHelper.RefreshTileTraversalFlags(ship);
			ApplyPlaceholderCrewCollisionIgnores(ship);
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException("Patch_PlaceholderCrewCollision_Common.RefreshShipTraversal", ex);
		}
	}

	private static void ApplyPlaceholderCrewCollisionIgnores(Ship ship)
	{
		if (ship == null)
		{
			return;
		}

		List<CondOwner> placeholders = new List<CondOwner>();
		List<CondOwner> crew = new List<CondOwner>();

		foreach (CondOwner co in ship.GetCOs(null, true, false, true))
		{
			if (co == null)
			{
				continue;
			}

			if (PlaceholderTraversalHelper.IsUninstalledPlaceholder(co))
			{
				placeholders.Add(co);
				continue;
			}

			if (PlaceholderTraversalHelper.IsCrew(co))
			{
				crew.Add(co);
			}
		}

		for (int i = 0; i < placeholders.Count; i++)
		{
			Collider[] placeholderColliders = placeholders[i].GetComponentsInChildren<Collider>(true);
			if (placeholderColliders == null || placeholderColliders.Length == 0)
			{
				continue;
			}

			for (int j = 0; j < crew.Count; j++)
			{
				Collider[] crewColliders = crew[j].GetComponentsInChildren<Collider>(true);
				if (crewColliders == null || crewColliders.Length == 0)
				{
					continue;
				}

				for (int p = 0; p < placeholderColliders.Length; p++)
				{
					Collider placeholderCollider = placeholderColliders[p];
					if (placeholderCollider == null)
					{
						continue;
					}

					for (int c = 0; c < crewColliders.Length; c++)
					{
						Collider crewCollider = crewColliders[c];
						if (crewCollider == null || ReferenceEquals(placeholderCollider, crewCollider))
						{
							continue;
						}

						Physics.IgnoreCollision(placeholderCollider, crewCollider, true);
					}
				}
			}
		}
	}
}
