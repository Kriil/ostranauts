using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduitStart : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Loose Conduit - Hidden";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return string.Empty;
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "HallwayConduit";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit2";
			}
		}

		public override void MakeObjective()
		{
		}

		public void Enter(JsonZone jsonZone)
		{
			if (jsonZone.strName == "trigger hallway conduit")
			{
				if (CrewSim.coPlayer != null && CrewSim.coPlayer.Pathfinder != null)
				{
					CrewSim.coPlayer.Pathfinder.RemoveTriggerListener(new UnityAction<JsonZone>(this.Enter));
				}
				base.Finished = true;
			}
		}

		public override void AddInitialListeners()
		{
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.Pathfinder != null)
			{
				CrewSim.coPlayer.Pathfinder.AddTriggerListener(new UnityAction<JsonZone>(this.Enter));
			}
		}

		public override void RemoveAllListeners()
		{
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.Pathfinder != null)
			{
				CrewSim.coPlayer.Pathfinder.RemoveTriggerListener(new UnityAction<JsonZone>(this.Enter));
			}
		}
	}
}
