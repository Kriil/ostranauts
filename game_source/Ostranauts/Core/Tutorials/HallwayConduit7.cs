using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class HallwayConduit7 : TutorialBeat
	{
		public HallwayConduit7()
		{
			this.SetPersistentRef("IsTutorialHallwayRack", this.rack);
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.Pathfinder != null)
			{
				CrewSim.coPlayer.Pathfinder.AddTriggerListener(new UnityAction<JsonZone>(this.Enter));
			}
		}

		public override string ObjectiveName
		{
			get
			{
				return "Search The Rack";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "It's somewhere in the dark ...";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Nyctophobia overcome.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "HallwayConduit8";
			}
		}

		public override CondOwner COTarget
		{
			get
			{
				return this.rack.CO;
			}
		}

		public void Enter(JsonZone jsonZone)
		{
			if (jsonZone.strName == "trigger hallway reward")
			{
				if (CrewSim.coPlayer != null && CrewSim.coPlayer.Pathfinder != null)
				{
					CrewSim.coPlayer.Pathfinder.RemoveTriggerListener(new UnityAction<JsonZone>(this.Enter));
				}
				GUIQuickBar.OnQABButtonClicked = (UnityAction<GUIQuickActionButton>)Delegate.Combine(GUIQuickBar.OnQABButtonClicked, new UnityAction<GUIQuickActionButton>(this.OnQuickActionButton));
				base.MakeObjective();
			}
		}

		private void OnQuickActionButton(GUIQuickActionButton qab)
		{
			if (qab != null)
			{
				Interaction ia = qab.IA;
				if (ia == null)
				{
					return;
				}
				if (ia.objThem == null)
				{
					return;
				}
				if (ia.objThem.strID == this.rack.CO.strID)
				{
					base.Finished = true;
				}
			}
		}

		public override void MakeObjective()
		{
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
			GUIQuickBar.OnQABButtonClicked = (UnityAction<GUIQuickActionButton>)Delegate.Remove(GUIQuickBar.OnQABButtonClicked, new UnityAction<GUIQuickActionButton>(this.OnQuickActionButton));
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.Pathfinder != null)
			{
				CrewSim.coPlayer.Pathfinder.RemoveTriggerListener(new UnityAction<JsonZone>(this.Enter));
			}
		}

		public UniqueIDCOPair rack = new UniqueIDCOPair();
	}
}
