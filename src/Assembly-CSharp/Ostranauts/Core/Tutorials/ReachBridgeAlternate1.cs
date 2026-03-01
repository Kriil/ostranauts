using System;

namespace Ostranauts.Core.Tutorials
{
	public class ReachBridgeAlternate1 : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Find Another Route";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Alternatively, if you hunt around, there might be another way past the door ...";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Now that's something to crow about.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ReachBridgeAlternate2";
			}
		}

		public override void Process()
		{
			CrewSimTut.UniqueToStrID.TryGetValue("TutorialDerelictCrowbar", out this.crowbarID);
			if (string.IsNullOrEmpty(this.crowbarID) || DataHandler.mapCOs.TryGetValue(this.crowbarID, out this.crowbar))
			{
			}
			if (this.crowbar && this.crowbar.RootParent(null) == CrewSim.coPlayer)
			{
				base.Finished = true;
			}
			base.Process();
		}

		private CondOwner crowbar;

		private string crowbarID;
	}
}
