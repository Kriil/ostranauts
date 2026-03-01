using System;
using UnityEngine;

public class PledgeFollow : Pledge2
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
		Vector2 pos = base.Us.GetPos("use", false);
		Vector2 pos2 = this.Them.GetPos(null, false);
		if (TileUtils.TileRange(pos, pos2) <= 5)
		{
			return true;
		}
		Tile tileAtWorldCoords = base.Us.ship.GetTileAtWorldCoords1(pos2.x, pos2.y, true, true);
		Pathfinder pathfinder = base.Us.Pathfinder;
		if (!(pathfinder != null))
		{
			return false;
		}
		PathResult pathResult = pathfinder.SetGoal2(tileAtWorldCoords, 0f, this.Them, pos2.x, pos2.y, base.Us.HasAirlockPermission(false));
		if (pathResult.HasPath)
		{
			pathfinder.VisualisePath(pathfinder.currentPath);
			Interaction interaction = DataHandler.GetInteraction("Walk", null, false);
			if (base.Us.QueueInteraction(base.Us, interaction, false))
			{
				interaction.objThem = tileAtWorldCoords.coProps;
				interaction.strTargetPoint = "use";
				interaction.fTargetPointRange = 0f;
			}
			return true;
		}
		return false;
	}
}
