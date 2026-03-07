using System;
using UnityEngine.Events;

namespace Ostranauts.Core.Tutorials
{
	public class OpenInventory : TutorialBeat
	{
		public override string ObjectiveName
		{
			get
			{
				return "Open Your Inventory";
			}
		}

		public override string ObjectiveDesc
		{
			get
			{
				return "Click your portrait, or press '" + GUIActionKeySelector.commandInventory.KeyName + "' to take a look at the items you have.";
			}
		}

		public override string ObjectiveDescComplete
		{
			get
			{
				return "Inventory viewed.";
			}
		}

		public override string NextDefault
		{
			get
			{
				return "GetDressed";
			}
		}

		public override bool CheckUserPreSatisfied()
		{
			if (CrewSim.coPlayer != null && !CrewSim.coPlayer.HasCond("TutorialStillInDorm"))
			{
				this.NextOverride = "NavWalk";
				return true;
			}
			return false;
		}

		public override void AddInitialListeners()
		{
			GUIInventory.OnOpenInventory.AddListener(new UnityAction<GUIInventoryWindow>(this.OnOpenInventory));
		}

		public void OnOpenInventory(GUIInventoryWindow gUIInventoryWindow)
		{
			if (gUIInventoryWindow != null && gUIInventoryWindow.CO && gUIInventoryWindow.CO.RootParent(null) == CrewSim.coPlayer)
			{
				base.Finished = true;
			}
		}

		public override void RemoveAllListeners()
		{
			GUIInventory.OnOpenInventory.RemoveListener(new UnityAction<GUIInventoryWindow>(this.OnOpenInventory));
		}
	}
}
