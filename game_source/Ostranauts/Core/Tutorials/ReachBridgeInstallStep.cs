using System;

namespace Ostranauts.Core.Tutorials
{
	public class ReachBridgeInstallStep : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Access The Bridge 3";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Open your vizor and identify the missing circuit link. Install the Loose Conduit to run power to the Bridge Door.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Bridge door powered.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ReachBridgeDoorStep";
			}
		}

		public override void Process()
		{
			CrewSimTut.UniqueToStrID.TryGetValue("TutorialDerelictDoorBridge", out this.doorID);
			if (!this.door && !string.IsNullOrEmpty(this.doorID))
			{
				DataHandler.mapCOs.TryGetValue(this.doorID, out this.door);
			}
			if (this.door && this.door.HasCond("IsPowered"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		private CondOwner door;

		private string doorID;
	}
}
