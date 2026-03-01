using System;

namespace Ostranauts.Core.Tutorials
{
	public class ExploreTutorialDerelict : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Explore Derelict";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Move the handheld lamp into your hand, toggle it on. Step into the derelict.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "First derelict visited.";
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
				return "PickUpPermit";
			}
		}

		public override void Process()
		{
			if (CrewSimTut.tutorialShipInstanceRef != null && CrewSim.coPlayer.ship == CrewSimTut.tutorialShipInstanceRef)
			{
				base.Finished = true;
			}
			base.Process();
		}
	}
}
