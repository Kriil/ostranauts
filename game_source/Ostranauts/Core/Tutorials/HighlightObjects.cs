using System;

namespace Ostranauts.Core.Tutorials
{
	public class HighlightObjects : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Highlight Interactables & Hotkeys";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Hold " + GUIActionKeySelector.CommandShowHotkeys.KeyName + " to see hotkeys and nearby interactable objects.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Interactables & hotkeys highlighted.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "OpenInventory";
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			if (CrewSim.coPlayer != null && !CrewSim.coPlayer.HasCond("TutorialStillInDorm"))
			{
				this.NextOverride = "NavWalk";
				return true;
			}
			return false;
		}

		public override void Process()
		{
			if (GUIActionKeySelector.CommandShowHotkeys.Down)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
