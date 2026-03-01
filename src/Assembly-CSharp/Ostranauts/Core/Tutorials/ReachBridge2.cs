using System;

namespace Ostranauts.Core.Tutorials
{
	public class ReachBridge2 : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Access The Bridge 2";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Uninstall the Conduit via the QAB.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Conduit uninstalled.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ReachBridgeInstallStep";
			}
		}

		public override void Process()
		{
			CrewSimTut.UniqueToStrID.TryGetValue("TutorialDerelictConduit2", out this.conduitID);
			if (!this.conduit && !string.IsNullOrEmpty(this.conduitID))
			{
				DataHandler.mapCOs.TryGetValue(this.conduitID, out this.conduit);
			}
			if (this.conduit && !this.conduit.HasCond("IsInstalled"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		private string conduitID;

		private CondOwner conduit;
	}
}
