using System;
using System.Collections.Generic;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;

namespace Ostranauts.Ships.AIPilots
{
	public class StationPilot : IAICharacter
	{
		public StationPilot(Ship ship)
		{
			this.ShipUs = ship;
			if (this.ShipUs == null)
			{
				return;
			}
			this.Commands = new List<ICommand>
			{
				new Blank(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.Station;
			}
		}

		public double MaxSpeed(bool? inAtmo = false)
		{
			return 5.013440183831985E-09;
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			return lastActiveCommand;
		}

		public TargetData GetTarget()
		{
			return new TargetData(this.ShipUs.objSS);
		}
	}
}
