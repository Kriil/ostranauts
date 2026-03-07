using System;
using System.Collections.Generic;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Ships.Commands;

namespace Ostranauts.Ships.AIPilots
{
	public class AutoPilot : IAICharacter
	{
		public AutoPilot(string strStationID = null, Ship ship = null)
		{
			this.ShipUs = ship;
			if (this.ShipUs == null)
			{
				return;
			}
			this.Commands = new List<ICommand>
			{
				new FlyToAutoPilot(this),
				new RemoveAI(this)
			};
		}

		public Ship ShipUs { get; private set; }

		public List<ICommand> Commands { get; set; }

		public AIType AIType
		{
			get
			{
				return AIType.Auto;
			}
		}

		public double MaxSpeed(bool? inAtmo = null)
		{
			bool flag = (inAtmo == null) ? this.ShipUs.InAtmo : inAtmo.Value;
			return (!flag) ? 5.013440183831985E-09 : 2.5067200919159927E-09;
		}

		public ICommand FFWD(ICommand lastActiveCommand)
		{
			if (lastActiveCommand is FlyToAutoPilot && this.ShipUs.objSS.HasNavData())
			{
				return lastActiveCommand;
			}
			lastActiveCommand.ResolveInstantly();
			return lastActiveCommand;
		}

		public TargetData GetTarget()
		{
			if (this.ShipUs.shipScanTarget != null)
			{
				return new TargetData(this.ShipUs.shipScanTarget);
			}
			if (this.ShipUs.shipSituTarget != null)
			{
				return new TargetData(this.ShipUs.shipSituTarget);
			}
			return null;
		}
	}
}
