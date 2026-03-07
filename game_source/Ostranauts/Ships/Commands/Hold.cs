using System;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class Hold : BaseCommand
	{
		public Hold(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (base.ShipUs.DeltaVRemainingRCS > 0.0) ? "Holding position" : "Out of fuel";
			}
		}

		public override CommandCode RunCommand()
		{
			base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
			if (base.ShipUs.fAIPauseTimer > 0.0 && base.ShipUs.fAIDockingExpire == 0.0)
			{
				base.ShipUs.objSS.bIgnoreGrav = false;
				return CommandCode.Skipped;
			}
			if (double.IsNegativeInfinity(base.ShipUs.fAIDockingExpire))
			{
				base.ShipUs.fAIDockingExpire = (double)MathUtils.Rand(100, 650, MathUtils.RandType.Flat, null);
			}
			base.ShipUs.fAIDockingExpire -= (double)CrewSim.TimeElapsedScaled();
			base.ShipUs.fAIPauseTimer -= (double)CrewSim.TimeElapsedScaled();
			if (base.ShipUs.fAIPauseTimer < 0.0)
			{
				base.ShipUs.fAIPauseTimer = 0.0;
			}
			if (base.ShipUs.fAIDockingExpire > 0.0)
			{
				base.ShipUs.objSS.bIgnoreGrav = true;
				return CommandCode.Ongoing;
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsHasRequestedHelp");
			CondOwner objOwner;
			base.ShipUs.Comms.GetCaptain(out objOwner);
			if (condTrigger.Triggered(objOwner, null, true) || (base.ShipUs.GetRCSRemain() < 1.0 && base.ShipUs.LiftRotorsThrustStrength <= 0f))
			{
				base.ShipUs.fAIDockingExpire = double.NegativeInfinity;
				return CommandCode.Ongoing;
			}
			base.ShipUs.objSS.bIgnoreGrav = false;
			base.ShipUs.fAIDockingExpire = double.NegativeInfinity;
			return CommandCode.Finished;
		}

		public override CommandCode ResolveInstantly()
		{
			base.ShipUs.fAIDockingExpire = (double)MathUtils.Rand(1, 30, MathUtils.RandType.Flat, null);
			return CommandCode.Ongoing;
		}
	}
}
