using System;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class Blank : BaseCommand
	{
		public Blank(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			base.ShipUs.fAIDockingExpire = 10.0;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Maintaining systems";
			}
		}

		public override CommandCode RunCommand()
		{
			base.ShipUs.fAIDockingExpire -= (double)CrewSim.TimeElapsedScaled();
			if (base.ShipUs.fAIDockingExpire > 0.0)
			{
				return CommandCode.Ongoing;
			}
			base.ShipUs.fAIDockingExpire = double.NegativeInfinity;
			AIShipManager.UnregisterShip(base.ShipUs);
			return CommandCode.Finished;
		}
	}
}
