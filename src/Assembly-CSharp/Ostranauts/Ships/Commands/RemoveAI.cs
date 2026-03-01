using System;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class RemoveAI : BaseCommand
	{
		public RemoveAI(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Removing AI " + base.ShipUs;
			}
		}

		public override CommandCode RunCommand()
		{
			AIShipManager.UnregisterShip(base.ShipUs);
			return CommandCode.Finished;
		}
	}
}
