using System;

namespace Ostranauts.Core.Tutorials
{
	public class ForwardThrust : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Calibrate Forward Thrust";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press the \"" + GUIActionKeySelector.commandFlyUp.KeyName + "\" key to reduce VREL below 10 m/s again (with OKLG targeted).";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Forward Thrust Calibrated.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "RightThrust";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public override void Process()
		{
			if (GUIOrbitDraw.Instance && Math.Abs(GUIOrbitDraw.Instance.fVRel) <= 6.68458706417963E-11)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
