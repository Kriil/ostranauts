using System;

namespace Ostranauts.Core.Tutorials
{
	public class RearThrust : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Calibrate Rear Thrust";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press the \"" + GUIActionKeySelector.commandFlyDown.KeyName + "\" key to thrust backward up to 200 m/s VREL (with OKLG targeted).";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Rear Thrust Calibrated.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ForwardThrust";
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
