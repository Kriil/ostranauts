using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs;
using UnityEngine;

[Serializable]
public class CommandInventory : Command
{
	public CommandInventory()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.I);
		this.commandDisplayLabel = "Player Inventory";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER)
		{
			return;
		}
		if (CrewSim.bRaiseUI)
		{
			return;
		}
		if (CrewSim.Typing || CrewSim.bShipEdit)
		{
			return;
		}
		if (base.Down)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
	}

	public static void ToggleInventory(CondOwner coUs, bool bForce = false)
	{
		GUIInventory inventoryGUI = CrewSim.inventoryGUI;
		bool isOpen = GUIInventory.instance.IsOpen;
		if (!bForce && isOpen && GUIInventory.instance.Selected != null)
		{
			CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_INV_NO_CLOSE", false), "Bad", CrewSim.GetSelectedCrew().strID);
			AudioManager.am.PlayAudioEmitter("UIMessageLogBad", false, true);
			return;
		}
		CanvasManager.HideCanvasGroup(inventoryGUI.PaperDollImageCG);
		for (int i = inventoryGUI.activeWindows.Count - 1; i >= 0; i--)
		{
			inventoryGUI.Close(inventoryGUI.activeWindows[i]);
			GUIInventory.RemoveTooltip(null);
		}
		if (!isOpen)
		{
			CrewSim.LowerUI(false);
			GUIZones.CloseMenu();
			CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
			inventoryGUI.SpawnInventoryWindow(coUs, InventoryWindowType.Container, null, null);
			inventoryGUI.PaperDollImageCG.GetComponent<GUIPaperDollManager>().SetPaperDoll(coUs);
			CanvasManager.instance.ToggleInventoryVisibility(true);
			CanvasManager.ShowCanvasGroup(inventoryGUI.PaperDollImageCG);
			if (CrewSim.inventoryGUI.Selected != null)
			{
				CrewSim.inventoryGUI.Selected.transform.SetSiblingIndex(CrewSim.inventoryGUI.transform.childCount - 1);
				CrewSim.inventoryGUI.Selected.AttachToCursor(null);
			}
			else
			{
				CrewSim.inventoryGUI.RestoreSelected();
			}
			GUIInventory.instance.JustClickedItem = true;
			Debug.Log("OPEN INVENTORY");
		}
		else
		{
			CanvasManager.instance.ToggleInventoryVisibility(false);
			GUIInventory.instance.Reset(null);
			Debug.Log("CLOSE INVENTORY");
		}
		coUs.ZeroCondAmount("TutorialInvWaiting");
		if (GUIActionKeySelector.commandInventory.objOpenInv != null)
		{
			MonoSingleton<ObjectiveTracker>.Instance.RemoveObjective(GUIActionKeySelector.commandInventory.objOpenInv, ObjectiveTracker.REASON_COMPLETED, true);
			GUIActionKeySelector.commandInventory.objOpenInv = null;
		}
	}

	public Objective objOpenInv;
}
