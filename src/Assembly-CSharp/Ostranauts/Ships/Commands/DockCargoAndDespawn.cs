using System;
using System.Collections.Generic;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Trading;

namespace Ostranauts.Ships.Commands
{
	public class DockCargoAndDespawn : BaseCommand
	{
		public DockCargoAndDespawn(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			this._ai = pilot;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Docking with " + base.ShipUs.shipScanTarget.strRegID;
			}
		}

		public override CommandCode RunCommand()
		{
			if (base.ShipUs.shipScanTarget == null)
			{
				return CommandCode.Skipped;
			}
			Ship shipScanTarget = base.ShipUs.shipScanTarget;
			if (!shipScanTarget.IsStation(false) || !DockCargoAndDespawn.IsWithinRangeSpeed(base.ShipUs.objSS, shipScanTarget.objSS))
			{
				return CommandCode.Cancelled;
			}
			if (!base.ShipUs.Comms.AIGetClearance(shipScanTarget))
			{
				return CommandCode.Ongoing;
			}
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			base.ShipUs.bTowBraceSecured = false;
			this.TransferMarketCargo();
			if (allDockedShips.Count == 0)
			{
				CrewSim.DockAndDespawn(base.ShipUs, shipScanTarget, null);
				base.ShipUs.shipScanTarget = null;
				base.ShipUs.shipSituTarget = null;
				return CommandCode.Finished;
			}
			this.HandleDockedShips(allDockedShips, shipScanTarget);
			CrewSim.DockAndDespawn(base.ShipUs, shipScanTarget, null);
			base.ShipUs.shipScanTarget = null;
			return CommandCode.Finished;
		}

		private void HandleDockedShips(List<Ship> dockedShips, Ship shipTarget)
		{
			foreach (Ship ship in dockedShips)
			{
				if (ship != null)
				{
					bool flag = CrewSim.coPlayer.OwnsShip(ship.strRegID);
					CondOwner coUs;
					ship.Comms.GetCaptain(out coUs);
					DataHandler.GetLoot("ShipRemoveHelpRequest").ApplyCondLoot(coUs, 1f, null, 0f);
					base.ShipUs.Undock(ship);
					if (!flag)
					{
						CrewSim.DockAndDespawn(ship, shipTarget, base.ShipUs.strRegID);
						if (ship.IsDerelict())
						{
							ship.Destroy(false);
						}
					}
					else if (ship.LoadState > Ship.Loaded.Shallow)
					{
						BeatManager.RunEncounter("ENCHauledHome", false);
						CrewSim.UndockShip(ship, base.ShipUs, false, false);
						base.ShipUs = CrewSim.system.GetShipByRegID(base.ShipUs.strRegID);
						CrewSim.DockShip(ship, shipTarget.strRegID);
					}
					else if (shipTarget.LoadState > Ship.Loaded.Shallow)
					{
						CrewSim.DockShip(shipTarget, ship.strRegID);
					}
					else
					{
						ship.Dock(shipTarget, false);
					}
				}
			}
		}

		public override CommandCode ResolveInstantly()
		{
			if (base.ShipUs.shipScanTarget == null)
			{
				return CommandCode.Cancelled;
			}
			base.PlaceWithinDockingRange(base.ShipUs.shipScanTarget.objSS);
			base.ShipUs.bTowBraceSecured = false;
			this.TransferMarketCargo();
			this.HandleDockedShips(base.ShipUs.GetAllDockedShips(), base.ShipUs.shipScanTarget);
			CrewSim.DockAndDespawn(base.ShipUs, base.ShipUs.shipScanTarget, null);
			base.ShipUs.shipScanTarget = null;
			return CommandCode.Finished;
		}

		private void TransferMarketCargo()
		{
			MarketManager.UnregisterCargoShip(base.ShipUs.strRegID, base.ShipUs.shipScanTarget.strRegID);
		}

		public static bool IsWithinRangeSpeed(ShipSitu situUs, ShipSitu target)
		{
			if (situUs == null || target == null)
			{
				return false;
			}
			float num = (float)situUs.GetRangeTo(target);
			double dX = target.vVelX - situUs.vVelX;
			double dY = target.vVelY - situUs.vVelY;
			double magnitude = MathUtils.GetMagnitude(dX, dY);
			double num2 = magnitude * 149597872.0 * 1000.0;
			return num < 1.0026881E-07f && num2 < 100.0;
		}

		private readonly IAICharacter _ai;
	}
}
