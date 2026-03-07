using System;

namespace Ostranauts.Core.Tutorials
{
	public class BlankTutorial : TutorialBeat
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
				return "TutorialStub";
			}
		}

		public override void Process()
		{
			base.Process();
		}
	}
}
