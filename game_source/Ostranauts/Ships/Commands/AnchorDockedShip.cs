using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class AnchorDockedShip : Undock
	{
		public AnchorDockedShip(IAICharacter pilot) : base(pilot)
		{
			base.ShipUs = pilot.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Anchoring docked ship";
			}
		}

		public override CommandCode RunCommand()
		{
			List<Ship> allDockedShips = base.ShipUs.GetAllDockedShips();
			Ship ship = allDockedShips.FirstOrDefault<Ship>();
			if (allDockedShips.Count != 0)
			{
				BodyOrbit nearestBO = CrewSim.system.GetNearestBO(ship.objSS, StarSystem.fEpoch, true);
				ship.objSS.UpdateTime(StarSystem.fEpoch, false);
				ship.objSS.LockToBO(nearestBO, -1.0);
				CommandCode result = base.RunCommand();
				ship.objSS.ssDockedHeavier = null;
				string origin = base.ShipUs.origin;
				Ship shipByRegID = CrewSim.system.GetShipByRegID(origin);
				CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsAllowsAnchoring");
				bool flag = shipByRegID != null && condTrigger.Triggered(shipByRegID.ShipCO, null, true);
				if (flag)
				{
					ship.objSS.LockToBO(nearestBO, -1.0);
				}
				else
				{
					ship.objSS.bBOLocked = false;
				}
				return result;
			}
			return base.RunCommand();
		}

		public override CommandCode ResolveInstantly()
		{
			base.PlaceWithinDockingRange(base.ShipUs.shipSituTarget);
			return this.RunCommand();
		}
	}
}
