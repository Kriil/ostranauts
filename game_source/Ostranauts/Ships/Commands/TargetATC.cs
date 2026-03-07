using System;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class TargetATC : BaseCommand
	{
		public TargetATC(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Targeting " + base.ShipUs.shipScanTarget;
			}
		}

		public override CommandCode RunCommand()
		{
			Ship ship = CrewSim.system.GetNearestStationRegional(base.ShipUs.objSS.vPosx, base.ShipUs.objSS.vPosy);
			if (ship == null)
			{
				ship = AIShipManager.ShipATCLast;
			}
			if (ship != null)
			{
				base.ShipUs.shipSituTarget = ship.objSS;
				base.ShipUs.shipScanTarget = ship;
			}
			return CommandCode.Finished;
		}

		public override CommandCode ResolveInstantly()
		{
			return this.RunCommand();
		}
	}
}
