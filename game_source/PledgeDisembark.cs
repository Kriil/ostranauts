using System;
using System.Collections.Generic;

public class PledgeDisembark : Pledge2
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
			return true;
		}
		if (!base.Triggered())
		{
			return false;
		}
		if (base.Us.ship == null || this.Them.ship == null)
		{
			return false;
		}
		List<Ship> list = new List<Ship>();
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(this.Them.strID);
		bool flag = true;
		foreach (string strRegID in shipsForOwner)
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
			if (shipByRegID != null)
			{
				list.Add(shipByRegID);
				if (shipByRegID == base.Us.ship)
				{
					flag = false;
				}
			}
		}
		if (flag)
		{
			base.Us.SetCondAmount("IsDisembarkCommand", 0.0, 0.0);
			return true;
		}
		foreach (Ship ship in base.Us.ship.GetAllDockedShips())
		{
			if (!list.Contains(ship))
			{
				Tile crewSpawnTile = ship.GetCrewSpawnTile(base.Us);
				Pathfinder pathfinder = base.Us.Pathfinder;
				if (pathfinder != null)
				{
					PathResult pathResult = pathfinder.SetGoal2(crewSpawnTile, 0f, crewSpawnTile.coProps, crewSpawnTile.tf.position.x, crewSpawnTile.tf.position.y, base.Us.HasAirlockPermission(false));
					if (pathResult.HasPath)
					{
						pathfinder.VisualisePath(pathfinder.currentPath);
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
			}
		}
		return false;
	}
}
