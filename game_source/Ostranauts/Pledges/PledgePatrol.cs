using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

namespace Ostranauts.Pledges
{
	public class PledgePatrol : Pledge2
	{
		public override bool Do()
		{
			if (base.Us == null)
			{
				return false;
			}
			if (this.Finished())
			{
				return true;
			}
			if (this._combatCT == null)
			{
				this._combatCT = DataHandler.GetCondTrigger("TIsInCombat");
			}
			if (this._combatCT.Triggered(base.Us, null, true))
			{
				return false;
			}
			List<JsonZone> zones = base.Us.ship.GetZones("IsZoneTrigger", base.Us, false, false);
			if (zones != null && zones.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, zones.Count);
				int[] aTiles = zones[index].aTiles;
				if (aTiles != null)
				{
					int index2 = aTiles.ToList<int>().Randomize<int>().FirstOrDefault<int>();
					Tile target = base.Us.ship.aTiles[index2];
					return this.SetTarget(target);
				}
			}
			foreach (Room room in base.Us.ship.aRooms.Randomize<Room>())
			{
				if (!room.Void && UnityEngine.Random.Range(0, 2) != 0)
				{
					Tile target2 = room.aTiles.Randomize<Tile>().FirstOrDefault<Tile>();
					return this.SetTarget(target2);
				}
			}
			return false;
		}

		private bool SetTarget(Tile targetTile)
		{
			Pathfinder pathfinder = base.Us.Pathfinder;
			if (pathfinder != null)
			{
				PathResult pathResult = pathfinder.SetGoal2(targetTile, 0f, targetTile.coProps, targetTile.tf.position.x, targetTile.tf.position.y, base.Us.HasAirlockPermission(false));
				if (pathResult.HasPath)
				{
					pathfinder.VisualisePath(pathfinder.currentPath);
					Interaction interaction = DataHandler.GetInteraction("Walk", null, false);
					if (base.Us.QueueInteraction(base.Us, interaction, false))
					{
						interaction.objThem = targetTile.coProps;
						interaction.strTargetPoint = "use";
						interaction.fTargetPointRange = 0f;
						return true;
					}
				}
			}
			return false;
		}

		private CondTrigger _combatCT;
	}
}
