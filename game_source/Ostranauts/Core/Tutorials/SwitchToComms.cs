using System;

namespace Ostranauts.Core.Tutorials
{
	public class SwitchToComms : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Switch to Comms Controls";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click the \"Comms Controls\" button on the right side of the Nav console to see the Comms Controls.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Switched to Comms Screen.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HailShip";
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
			if (GUIDockSys.instance && GUIDockSys.instance.bActive)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
