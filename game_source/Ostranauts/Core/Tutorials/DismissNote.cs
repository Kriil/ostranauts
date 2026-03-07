using System;

namespace Ostranauts.Core.Tutorials
{
	public class DismissNote : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Dismiss Note";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click on the paper Cheat Sheet note to move it aside.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Cheat Sheet Dismissed";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "Zoom";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return CrewSimTut.playerShipNavStationRef;
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			return (GUIOrbitDraw.Instance && GUIOrbitDraw.Instance.bActive && !GUIOrbitDraw.Instance.NoteShowing) || base.CheckUserPreSatisfied();
		}

		public override void Process()
		{
			if (GUIOrbitDraw.Instance && GUIOrbitDraw.Instance.bActive && !GUIOrbitDraw.Instance.NoteShowing)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
