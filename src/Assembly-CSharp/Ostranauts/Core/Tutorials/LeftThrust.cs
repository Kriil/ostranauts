using System;

namespace Ostranauts.Core.Tutorials
{
	public class LeftThrust : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Calibrate Left Thrust";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press the \"" + GUIActionKeySelector.commandFlyLeft.KeyName + "\" key to reduce VREL to below 100 m/s again (with OKLG targeted).";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Left Thrust Calibrated.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "TargetDerelict";
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
			if (GUIOrbitDraw.Instance && Math.Abs(GUIOrbitDraw.Instance.fVRel) <= 6.684587064179629E-10)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
