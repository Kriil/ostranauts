using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.ShipGUIs.Utilities;

namespace Ostranauts.ShipGUIs.MFD
{
	public class MFDDockingClearance : MFDPage
	{
		public MFDDockingClearance(int subPage = 0)
		{
			List<ShipDist> list = new List<ShipDist>();
			if (base.ShipUs.bDocked)
			{
				foreach (Ship ship in base.ShipUs.GetAllDockedShips())
				{
					list.Add(new ShipDist
					{
						strRegID = ship.strRegID,
						fDist = base.ShipUs.objSS.GetRangeTo(ship.objSS),
						si = ShipInfo.GetShipInfo(base.ShipUs, ship, GUIDockSys.DictGPM)
					});
				}
			}
			else
			{
				foreach (Ship ship2 in CrewSim.system.dictShips.Values)
				{
					if (ship2 != base.ShipUs && !ship2.bDestroyed && !ship2.HideFromSystem && !ship2.IsStationHidden(false))
					{
						ShipDist item = default(ShipDist);
						item.strRegID = ship2.strRegID;
						item.fDist = base.ShipUs.objSS.GetRangeTo(ship2.objSS);
						float collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(base.ShipUs, ship2);
						item.fDist -= (double)collisionDistanceAU;
						item.si = ShipInfo.GetShipInfo(base.ShipUs, ship2, GUIDockSys.DictGPM);
						bool flag = false;
						if (item.fDist <= 3.342293553032505E-08)
						{
							for (int i = 0; i < list.Count; i++)
							{
								if (list[i].fDist >= item.fDist)
								{
									list.Insert(i, item);
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								list.Add(item);
							}
						}
					}
				}
			}
			this._ships.Clear();
			this._ships.AddRange(list);
			base.PopulateMFD(this._ships, subPage);
		}

		public override MFDPage OnButtonDown(int btnIndex)
		{
			if ((btnIndex == 4 || btnIndex == 10) && this._ships.Count > 8)
			{
				base.PopulateMFD(this._ships, (btnIndex != 4) ? this._subPage++ : this._subPage--);
				base.UpdateDisplay();
			}
			else if (btnIndex == this._mainMenuButton)
			{
				return new MFDMainMenu();
			}
			return this;
		}

		private List<ShipDist> _ships = new List<ShipDist>();

		private int _subPage;
	}
}
