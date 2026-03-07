using System;

namespace Ostranauts.Core.Tutorials
{
	public class UnpauseWorld : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Unpause World";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Press '" + GUIActionKeySelector.commandPause.KeyName + "' or the triangle \"play\" button in the lower right time bar.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "World unpaused.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HighlightObjects";
			}
		}

		public override void Process()
		{
			if (CrewSim.Paused)
			{
				this.enteredPause = true;
			}
			if (this.enteredPause && !CrewSim.Paused)
			{
				base.Finished = true;
			}
			base.Process();
		}

		private bool enteredPause;
	}
}
