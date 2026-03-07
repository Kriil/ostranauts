using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandDebug : Command
{
	public CommandDebug()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.BackQuote);
		this.commandDisplayLabel = "Debug:";
		this.vital = true;
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (!CrewSim.bEnableDebugCommands)
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
		if (base.Down)
		{
			if (CanvasManager.instance.goCanvasDebug.GetComponent<CanvasGroup>().alpha == 1f)
			{
				CrewSim.bDebugShow = false;
				CanvasManager.HideCanvasGroup(CanvasManager.instance.goCanvasDebug);
			}
			else
			{
				CrewSim.bDebugShow = true;
				CanvasManager.ShowCanvasGroup(CanvasManager.instance.goCanvasDebug);
				DebugFastTravel component = CanvasManager.instance.goCanvasDebug.transform.Find("DebugFastTravel").GetComponent<DebugFastTravel>();
				component.Init();
				DebugRespawnShip component2 = CanvasManager.instance.goCanvasDebug.transform.Find("DebugRespawnShip").GetComponent<DebugRespawnShip>();
				component2.Init();
			}
			if (CrewSim.shipCurrentLoaded != null)
			{
				List<Ship> list = CrewSim.shipCurrentLoaded.GetAllDockedShips();
				list.Add(CrewSim.shipCurrentLoaded);
				foreach (Ship ship in list)
				{
					ship.ShowRoomIDs(CrewSim.bDebugShow);
				}
				list.Clear();
				list = null;
			}
		}
	}
}
