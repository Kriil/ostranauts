using System;
using System.Collections.Generic;

namespace Ostranauts.Core.Tutorials
{
	public class RestorePower : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Install Conduit to restore door power.";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Restore power to the door by installing the loose conduit into the gap nearby.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "One door down.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "ReachBridgeTest";
			}
		}

		public override void Process()
		{
			if (!this.targetDoor)
			{
				string text = null;
				CrewSimTut.UniqueToStrID.TryGetValue("TutorialDerelictDoor1", out text);
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				DataHandler.mapCOs.TryGetValue(text, out this.targetDoor);
			}
			if (this.targetDoor && this.targetDoor.HasCond("IsPowered"))
			{
				base.Finished = true;
			}
			base.Process();
		}

		public override void OnFinish()
		{
			base.OnFinish();
			CrewSimTut.BeginTutorialBeat<ReachBridgeAlternate1>();
		}

		public int unpoweredDoorsCount = -1;

		public List<CondOwner> Doors = new List<CondOwner>();

		public CondOwner targetDoor;
	}
}
