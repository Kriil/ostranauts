using System;

namespace Ostranauts.Core.Tutorials
{
	public class SwitchNav : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Switch to Nav Screen";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click \"Switch to Nav Controls\" on the left side of the console to return to Nav controls.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Switched to Nav Screen.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "DismissNote";
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
			if (GUIOrbitDraw.Instance && GUIOrbitDraw.Instance.bActive)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
