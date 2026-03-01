using System;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class TargetPlayer : BaseCommand
	{
		public TargetPlayer(IAICharacter pilot)
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
			Ship ship = CrewSim.GetSelectedCrew().ship;
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
