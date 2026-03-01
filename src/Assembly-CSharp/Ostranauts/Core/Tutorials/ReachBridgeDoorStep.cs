using System;

namespace Ostranauts.Core.Tutorials
{
	public class ReachBridgeDoorStep : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Access The Bridge 4";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Walk through door.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Bridge reached.";
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
				return "LootTheBridge";
			}
		}

		public override void Process()
		{
			CrewSimTut.UniqueToStrID.TryGetValue("TutorialDerelictDoorBridge", out this.doorID);
			if (!this.door && !string.IsNullOrEmpty(this.doorID))
			{
				DataHandler.mapCOs.TryGetValue(this.doorID, out this.door);
			}
			if (this.door && this.door.HasCond("IsOpen"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		private CondOwner door;

		private string doorID;
	}
}
