using System;

namespace Ostranauts.Core.Tutorials
{
	public class PickUpPermit : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Pick Up Permit";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Pick up the yellow salvage permit on the ground. Right click it and select \"Read\".";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Retrieved permit.";
			}
		}

		public override string CTString
		{
			get
			{
				return "TIsTutorialExploreDerelictComplete";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "VisualisePower";
			}
		}

		public override void Process()
		{
			if (CrewSimTut.tutorialPermitRef != null && CrewSimTut.tutorialPermitRef.RootParent("IsPlayer"))
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
