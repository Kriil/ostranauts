using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.ConstructionTweaks;

internal static class PlaceholderTraversalHelper
{
	public static bool IsCrew(CondOwner co)
	{
		return co != null && (co.HasCond("IsHuman") || co.HasCond("IsRobot"));
	}

	public static bool IsUninstalledPlaceholder(CondOwner co)
	{
		return co != null && co.HasCond("IsPlaceholder") && !co.HasCond("IsInstalled");
	}

	public static bool IsPlaceholderOnlyBlockedTile(Tile tile)
	{
		try
		{
			if (tile?.coProps?.ship == null || tile.tf == null)
			{
				return false;
			}

			bool hasPlaceholder = false;
			bool hasRealBlockingObject = false;

			foreach (CondOwner co in tile.coProps.ship.GetCOs(null, true, false, true))
			{
				if (co == null || co == tile.coProps || !OccupiesTile(co, tile))
				{
					continue;
				}

			if (IsUninstalledPlaceholder(co))
			{
				hasPlaceholder = true;
				continue;
			}

			if (IsMovementBlockingOccupant(co))
			{
				hasRealBlockingObject = true;
				break;
			}
			}

			return hasPlaceholder && !hasRealBlockingObject;
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException(nameof(IsPlaceholderOnlyBlockedTile), ex);
			return false;
		}
	}

	private static bool OccupiesTile(CondOwner co, Tile tile)
	{
		if (co?.Item == null || tile == null)
		{
			return false;
		}

		Vector2 topLeft = co.TLTileCoords;
		float tileX = tile.tf.position.x;
		float tileY = tile.tf.position.y;

		return tileX >= topLeft.x
			&& tileX < topLeft.x + co.Item.nWidthInTiles
			&& tileY <= topLeft.y
			&& tileY > topLeft.y - co.Item.nHeightInTiles;
	}

	public static void RefreshTileTraversalFlags(Ship ship)
	{
		try
		{
			if (ship?.aTiles == null)
			{
				return;
			}

			for (int i = 0; i < ship.aTiles.Count; i++)
			{
				Tile tile = ship.aTiles[i];
				if (tile?.coProps == null)
				{
					continue;
				}

				bool placeholderOnly = IsPlaceholderOnlyBlockedTile(tile);
				if (placeholderOnly)
				{
					NeutralizePlaceholderTileBlocking(tile);
				}

				tile.bPassable = placeholderOnly || !tile.coProps.HasCond("IsObstruction", false);
			}
		}
		catch (System.Exception ex)
		{
			Plugin.LogPatchException(nameof(RefreshTileTraversalFlags), ex);
		}
	}

	private static bool IsMovementBlockingOccupant(CondOwner co)
	{
		if (co == null)
		{
			return false;
		}

		if (co.HasCond("IsWall") || co.HasCond("IsPortal"))
		{
			return true;
		}

		if (co.HasCond("IsFixture") && co.HasCond("IsObstruction"))
		{
			return true;
		}

		return co.HasCond("IsObstruction") && !co.HasCond("IsFloor") && !co.HasCond("IsTile");
	}

	private static void NeutralizePlaceholderTileBlocking(Tile tile)
	{
		if (tile?.coProps == null)
		{
			return;
		}

		List<string> wallConds = null;
		foreach (string condName in tile.coProps.mapConds.Keys)
		{
			if (condName == "IsObstruction" || condName == "IsFixture" || condName.StartsWith("IsWall"))
			{
				wallConds ??= new List<string>();
				wallConds.Add(condName);
			}
		}

		if (wallConds != null)
		{
			for (int i = 0; i < wallConds.Count; i++)
			{
				tile.coProps.ZeroCondAmount(wallConds[i]);
			}
		}

		tile.UpdateFlags();
	}
}
