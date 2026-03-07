using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.UI.Loading;
using Ostranauts.UI.MegaToolTip;
using Ostranauts.UI.Popups;
using UnityEngine;

[Serializable]
public class CommandEscape : Command
{
	public CommandEscape()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Escape);
		this.commandDisplayLabel = "Escape:";
		this.vital = true;
	}

	public override void Execute()
	{
		if (CommandEscape.bApplicationQuitInProgress)
		{
			return;
		}
		if (base.Down)
		{
			if (CrewSim.objInstance == null)
			{
				return;
			}
			if (CanvasManager.instance == null)
			{
				return;
			}
			if (CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER)
			{
				return;
			}
			if (CrewSim.Typing)
			{
				CrewSim.Typing = false;
				return;
			}
			CanvasGroup component = CanvasManager.instance.goCanvasQuit.transform.Find("GUIQuit/prefabGUIOptions").GetComponent<CanvasGroup>();
			CanvasGroup component2 = CanvasManager.instance.goCanvasQuit.transform.Find("GUIQuit/pnlManual").GetComponent<CanvasGroup>();
			GUISaveLoadBase componentInChildren = CanvasManager.instance.goCanvasQuit.transform.Find("GUIQuit").GetComponentInChildren<GUISaveLoadBase>();
			if (Info.instance.displayed)
			{
				Info.instance.Close();
				return;
			}
			MonoSingleton<GUIErrorPopUp>.Instance.CloseTooltip();
			if (!CrewSim.inventoryGUI.IsOpen && (CrewSim.objInstance.goSelPart != null || CrewSim.objInstance.goPaintJob != null || CrewSim.guiPDA.JobsActive))
			{
				CrewSim.guiPDA.HideJobPaintUI();
			}
			else if (CrewSim.guiPDA.State != GUIPDA.UIState.Closed)
			{
				CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
			}
			else if (CrewSim.bRaiseUI)
			{
				CrewSim.SetMainMenuOff();
				CrewSim.LowerUI(CrewSim.tplCurrentUI != null && CrewSim.tplCurrentUI.Item1 == "FFWD");
			}
			else if (GUIMegaToolTip.Selected != null)
			{
				CrewSim.OnRightClick.Invoke(new List<CondOwner>());
			}
			else if (CrewSim.inventoryGUI.IsOpen)
			{
				CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
			}
			else if (component != null && component.alpha == 1f)
			{
				component.GetComponentInChildren<GUIOptions>().Exit();
				CrewSim.SetMainMenuOff();
				CrewSim.LowerUI(false);
			}
			else if (componentInChildren != null)
			{
				UnityEngine.Object.Destroy(componentInChildren);
			}
			else if (component2 != null && component2.alpha > 0.5f)
			{
				component2.GetComponentInChildren<GUIManual>().Close();
				CrewSim.SetMainMenuOff();
				CrewSim.LowerUI(false);
			}
			else if (CrewSim.ZoneMenuOpen)
			{
				if (GUIActionKeySelector.OnKeyDown != null)
				{
					GUIActionKeySelector.OnKeyDown.Invoke(GUIActionKeySelector.commandToggleZoneUI.defaultCombo.FirstOrDefault<KeyCode>());
				}
			}
			else if (CrewSim.objInstance.coConnectMode != null)
			{
				CrewSim.objInstance.CloseConnectionMode();
			}
			else
			{
				CanvasManager.ToggleCanvasQuit();
			}
		}
	}

	public static bool bApplicationQuitInProgress;
}
