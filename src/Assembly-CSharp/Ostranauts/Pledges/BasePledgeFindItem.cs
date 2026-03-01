using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.Pledges
{
	public abstract class BasePledgeFindItem : Pledge2
	{
		private CondTrigger CtNotCarried
		{
			get
			{
				if (this._ctNotCarried == null)
				{
					this._ctNotCarried = DataHandler.GetCondTrigger("TIsNotCarried");
				}
				return this._ctNotCarried;
			}
		}

		protected abstract CondTrigger EmergencyConditions { get; }

		protected CondOwner FindItem(CondOwner coUs, CondTrigger ctItemType)
		{
			if (coUs == null || ctItemType == null || ctItemType.IsBlank())
			{
				return null;
			}
			CondOwner result = null;
			List<CondOwner> cos = coUs.GetCOs(false, ctItemType);
			if (cos != null && cos.Count > 0)
			{
				return cos[0];
			}
			if (coUs.ship == null)
			{
				return null;
			}
			List<Ship> list = new List<Ship>();
			bool flag = this.EmergencyConditions != null && this.EmergencyConditions.Triggered(coUs, null, true);
			bool flag2 = coUs.Company == null || coUs.Company.mapRoster[coUs.strID].bShoreLeave;
			if (flag || flag2)
			{
				foreach (Ship ship in coUs.ship.GetAllDockedShips())
				{
					if (flag || (flag2 && coUs.OwnsShip(ship.strRegID)))
					{
						list.Add(ship);
					}
				}
			}
			list.Insert(0, coUs.ship);
			foreach (Ship ship2 in list)
			{
				cos = ship2.GetCOs(ctItemType, true, false, false);
				foreach (CondOwner condOwner in cos)
				{
					if (this.CtNotCarried.Triggered(condOwner, null, true) && this.IsReachable(coUs, condOwner))
					{
						return condOwner;
					}
				}
			}
			return result;
		}

		private bool IsReachable(CondOwner objUs, CondOwner objThem)
		{
			Pathfinder pathfinder = objUs.Pathfinder;
			if (pathfinder != null && objThem != null)
			{
				Vector2 pos = objThem.GetPos("use", false);
				Tile tileAtWorldCoords = objUs.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
				bool bAllowAirlocks = objUs.HasAirlockPermission(false);
				PathResult pathResult = pathfinder.CheckGoal(tileAtWorldCoords, 0f, objThem, bAllowAirlocks);
				return pathResult.HasPath;
			}
			Debug.LogWarning("No Pathfinder or ConditionOwner, could not calculate path");
			return false;
		}

		private CondTrigger _ctNotCarried;
	}
}
