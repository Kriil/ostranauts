using System;

namespace Ostranauts.Core.Tutorials
{
	public class MatchSpeed : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Match Speed";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Toggle \"Match Speed\" (" + GUIActionKeySelector.commandShipMatchSpeed.KeyName + ") on to automatically match your targets speed and rotation.";
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
			if (GUIOrbitDraw.Instance != null && GUIOrbitDraw.Instance.chkStationKeeping != null && GUIOrbitDraw.Instance.chkStationKeeping.isOn && GUIOrbitDraw.Instance.fVRel < 50.0)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
