using System;

namespace Ostranauts.Core.Tutorials
{
	public class ToggleOffMatchSpeed : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Toggle Off MatchSpeed";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Remember to turn off match speed before moving the ship again.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Toggled match speed.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "DerelictComms";
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
			if (GUIOrbitDraw.Instance != null && GUIOrbitDraw.Instance.chkStationKeeping != null && !GUIOrbitDraw.Instance.chkStationKeeping.isOn)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
