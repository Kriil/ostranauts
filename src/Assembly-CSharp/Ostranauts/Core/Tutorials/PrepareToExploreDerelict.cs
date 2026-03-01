using System;

namespace Ostranauts.Core.Tutorials
{
	public class PrepareToExploreDerelict : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Put on Pressure Suit";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Leave the nav station, open inventory, remove your jumpsuit and shoes, and put on your pressure suit and helmet.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Space safe and vacuum ready.";
			}
		}

		public override string CTString
		{
			get
			{
				return "TIsWearingSpaceSuitAndHelmet";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ExploreTutorialDerelict";
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			this.WearingPressureSuit = DataHandler.GetCondTrigger(this.CTString);
			return this.WearingPressureSuit.Triggered(CrewSim.coPlayer, null, true);
		}

		public override void Process()
		{
			if (!base.Finished)
			{
				base.Finished = this.WearingPressureSuit.Triggered(CrewSim.coPlayer, null, true);
			}
			base.Process();
		}

		public CondTrigger WearingPressureSuit;
	}
}
