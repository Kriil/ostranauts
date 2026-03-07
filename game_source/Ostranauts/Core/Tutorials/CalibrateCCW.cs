using System;

namespace Ostranauts.Core.Tutorials
{
	public class CalibrateCCW : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Calibrate Counter-Clockwise Thrust";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Tap the \"" + GUIActionKeySelector.commandRotateCCW.KeyName + "\" until it is spinning counter-clockwise.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Counter-Clockwise Thrust Calibrated";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "StopSpin";
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
			if (CrewSimTut.playerShipRef != null && CrewSimTut.playerShipRef.objSS.fW > 0f)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
