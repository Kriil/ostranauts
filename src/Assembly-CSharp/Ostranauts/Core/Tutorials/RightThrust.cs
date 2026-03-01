using System;

namespace Ostranauts.Core.Tutorials
{
	public class RightThrust : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Calibrate Right Thrust";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press the \"" + GUIActionKeySelector.commandFlyRight.KeyName + "\" key to thrust right up to 200 m/s VREL (with OKLG targeted).";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Right Thrust Calibrated.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "LeftThrust";
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
			if (GUIOrbitDraw.Instance && Math.Abs(GUIOrbitDraw.Instance.fVRel) >= 1.3369174128359259E-09)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
