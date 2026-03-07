using System;

namespace Ostranauts.Core.Tutorials
{
	public class CalibrateCW : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Calibrate Clockwise Thrust";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Tap the \"" + GUIActionKeySelector.commandRotateCW.KeyName + "\" key to rotate your ship clockwise.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Clockwise Thrust Calibrated";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "CalibrateCCW";
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
			if (CrewSimTut.playerShipRef != null && CrewSimTut.playerShipRef.objSS.fW < 0f)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
