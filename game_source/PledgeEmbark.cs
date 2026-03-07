using System;
using System.Collections.Generic;
using UnityEngine;

public class PledgeEmbark : Pledge2
{
	public override bool Init(CondOwner coUs, JsonPledge jpIn, CondOwner coThem = null)
	{
		return base.Init(coUs, jpIn, coThem) && this.Them != null;
	}

	public override bool Init(string strUs, JsonPledge jpIn, string strThem = null)
	{
		return strThem != null && base.Init(strUs, jpIn, strThem);
	}

	public override bool Do()
	{
		if (base.Us == null || this.Them == null)
		{
			return false;
		}
		if (this.Finished())
		{
			CrewSim.objInstance.workManager.IdleRemove(base.Us);
			return true;
		}
		if (!base.Triggered())
		{
			return false;
		}
		if (base.Us.ship == null)
		{
			return false;
		}
		if (this.Them.OwnsShip(base.Us.ship.strRegID))
		{
			base.Us.SetCondAmount("IsEmbarkCommand", 0.0, 0.0);
			CrewSim.objInstance.workManager.IdleRemove(base.Us);
			return true;
		}
		List<Ship> list = new List<Ship>();
		List<CondOwner> list2 = null;
		Pathfinder pathfinder = base.Us.Pathfinder;
		if (pathfinder == null)
		{
			Debug.LogError("Error: PledgeReturnShip on non-pathfinder: " + base.Us);
		}
		foreach (string text in this.Them.GetShipsOwned())
		{
			if (text != null)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
				if (shipByRegID != null)
				{
					if (shipByRegID.LoadState >= Ship.Loaded.Edit)
					{
						if (this.CanWalkToShip(shipByRegID, pathfinder))
						{
							CrewSim.objInstance.workManager.IdleRemove(base.Us);
							return true;
						}
					}
					else
					{
						if (list2 == null)
						{
							list2 = base.Us.ship.GetCOs(DataHandler.GetCondTrigger("TIsTransit"), false, true, false);
						}
						if (list2.Count != 0)
						{
							list.Add(shipByRegID);
						}
					}
				}
			}
		}
		if (list2 == null || list2.Count == 0)
		{
			return false;
		}
		Ship ship = null;
		foreach (Ship shipDest in list)
		{
			ship = this.GetTransitShip(shipDest);
			if (ship != null)
			{
				break;
			}
		}
		if (ship == null)
		{
			return false;
		}
		float num = 0f;
		Interaction interaction = DataHandler.GetInteraction("PLGTransit", null, false);
		if (interaction != null)
		{
			num = interaction.fTargetPointRange;
		}
		CondOwner condOwner = list2[0];
		Vector2 pos = condOwner.GetPos("use", false);
		Tile tileAtWorldCoords = base.Us.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
		bool bAllowAirlocks = base.Us.HasAirlockPermission(false);
		PathResult pathResult = pathfinder.CheckGoal(tileAtWorldCoords, num, condOwner, bAllowAirlocks);
		if (!pathResult.HasPath)
		{
			return false;
		}
		if (pathResult.PathLength > num)
		{
			base.Us.AIIssueOrder(condOwner, interaction, this.Them == CrewSim.coPlayer, null, 0f, 0f);
			CrewSim.objInstance.workManager.IdleRemove(base.Us);
			return true;
		}
		CrewSim.MoveCO(base.Us, ship, false);
		if (!base.Us.HasQueuedInteraction("WanderSoon"))
		{
			Interaction interaction2 = DataHandler.GetInteraction("WanderSoon", null, false);
			base.Us.QueueInteraction(base.Us, interaction2, false);
		}
		CrewSim.objInstance.workManager.IdleRemove(base.Us);
		return true;
	}

	private bool CanWalkToShip(Ship ship, Pathfinder pf)
	{
		if (ship == null || pf == null)
		{
			return false;
		}
		Tile crewSpawnTile = ship.GetCrewSpawnTile(base.Us);
		if (pf != null)
		{
			PathResult pathResult = pf.SetGoal2(crewSpawnTile, 0f, crewSpawnTile.coProps, crewSpawnTile.tf.position.x, crewSpawnTile.tf.position.y, base.Us.HasAirlockPermission(false));
			if (pathResult.HasPath)
			{
				pf.VisualisePath(pf.currentPath);
				Interaction interaction = DataHandler.GetInteraction("Walk", null, false);
				if (base.Us.QueueInteraction(base.Us, interaction, false))
				{
					interaction.objThem = crewSpawnTile.coProps;
					interaction.strTargetPoint = "use";
					interaction.fTargetPointRange = 0f;
					return true;
				}
			}
		}
		return false;
	}

	private Ship GetTransitShip(Ship shipDest)
	{
		if (shipDest == null)
		{
			return null;
		}
		List<Ship> list = new List<Ship>();
		list.Add(shipDest);
		list.AddRange(shipDest.GetAllDockedShips());
		foreach (Ship ship in list)
		{
			if (JsonTransit.IsTransitConnected(ship.strRegID, base.Us.ship.strRegID))
			{
				return ship;
			}
		}
		return null;
	}
}
