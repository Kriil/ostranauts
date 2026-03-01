using System;

namespace Ostranauts.Core.Tutorials
{
	public class TutorialStub : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Tutorial Stub";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "This is the end of the tutorial development sequence.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Huh?!";
			}
		}
	}
}
