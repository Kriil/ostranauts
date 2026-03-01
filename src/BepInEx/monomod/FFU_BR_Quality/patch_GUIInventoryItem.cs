using System;
using FFU_Beyond_Reach;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;
public class patch_GUIInventoryItem : GUIInventoryItem
{
	public extern void orig_OnShiftPointerDown();
	public void OnShiftPointerDown()
	{
		bool flag = !FFU_BR_Defs.BetterInvTransfer || patch_GUIInventory.instance.targetWindow == null || patch_GUIInventory.instance.targetWindow.CO == null || this.windowData == patch_GUIInventory.instance.targetWindow;
		if (flag)
		{
			this.orig_OnShiftPointerDown();
		}
		else
		{
			CondOwner co = patch_GUIInventory.instance.targetWindow.CO;
			bool flag2 = this.windowData != null && this.windowData.type == 1 && base.CO.RootParent(null) == co;
			if (flag2)
			{
				bool flag3 = base.CO.objCOParent.DropCO(base.CO, false, null, 0f, 0f, true, null) != null;
				if (flag3)
				{
					base.AttachToCursor(null);
				}
			}
			else
			{
				bool flag4 = this.windowData == null && base.CO.objCOParent != null;
				if (flag4)
				{
					bool flag5 = base.CO.objCOParent.DropCO(base.CO, false, null, 0f, 0f, true, null) != null;
					if (flag5)
					{
						base.AttachToCursor(null);
					}
				}
				else
				{
					base.CO.RemoveFromCurrentHome(false);
					GUIInventory.RemoveTooltip(null);
					bool flag6 = this.windowData != null;
					if (flag6)
					{
						this.windowData.RemoveAndDestroy(base.CO.strID);
					}
					else
					{
						Object.Destroy(base.gameObject);
						CrewSim.objInstance.SetPartCursor(null);
					}
					CondOwner condOwner = co.AddCO(base.CO, true, true, false);
					bool flag7 = condOwner == null;
					if (flag7)
					{
						bool flag8 = GUIInventory.instance.IsCOShown(CrewSim.coPlayer) && CrewSim.coPlayer.HasCond("TutorialClothesWaiting");
						if (flag8)
						{
							CrewSim.coPlayer.ZeroCondAmount("TutorialClothesWaiting");
							MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
						}
						CrewSimTut.CheckHelmetAtmoTutorial();
						bool flag9 = GUIInventory.CTOpenInv.Triggered(base.CO, null, true);
						if (flag9)
						{
							GUIInventory.instance.SpawnInventoryWindow(base.CO, 1, null, null);
						}
						GUIInventory.instance.RedrawAllWindows();
					}
					else
					{
						GUIInventoryItem guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner.strID, null);
						if (guiinventoryItem != null)
						{
							guiinventoryItem.AttachToCursor(null);
						}
					}
				}
			}
		}
	}
	public extern bool orig_MoveInventories(GUIInventoryWindow destination, Vector2 position, bool canPlaceSelf);
	public bool MoveInventories(GUIInventoryWindow destination, Vector2 position, bool canPlaceSelf)
	{
		bool flag = this.orig_MoveInventories(destination, position, canPlaceSelf);
		bool flag2 = flag;
		if (flag2)
		{
			patch_GUIInventory.instance.targetWindow = destination;
		}
		return flag;
	}
}
