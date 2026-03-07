using System;

namespace Ostranauts.Core.Tutorials
{
	public class WaitForChargen : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Fill in the name of the Objective!";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Fill in the description text of the objective!";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Fill in the completion text of the objective!";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "UnpauseWorld";
			}
		}

		public override void MakeObjective()
		{
		}

		public override void Process()
		{
			if (CrewSim.coPlayer && !CrewSim.coPlayer.HasCond("IsInChargen"))
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
