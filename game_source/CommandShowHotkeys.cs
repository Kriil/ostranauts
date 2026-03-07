using System;
using System.Collections.Generic;
using Ostranauts.Components;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;

[Serializable]
public class CommandShowHotkeys : Command
{
	public CommandShowHotkeys()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.LeftAlt);
		this.commandDisplayLabel = "Show Hotkeys & Interactables:";
	}

	public override void Execute()
	{
		bool flag = false;
		if ((base.Down || base.Held) && HotkeyVisualizer.OnShowHotKeys != null)
		{
			HotkeyVisualizer.OnShowHotKeys.Invoke(Time.unscaledTime);
			flag = true;
		}
		if (CrewSim.objInstance == null)
		{
			return;
		}
		if (CrewSim.bRaiseUI)
		{
			CrewSim.objInstance.bHighlightInteractable = false;
			return;
		}
		if (base.Held)
		{
			CrewSim.objInstance.bHighlightInteractable = true;
			flag = true;
		}
		else
		{
			CrewSim.objInstance.bHighlightInteractable = false;
		}
		if (flag && CrewSim.coPlayer != null)
		{
			CrewSim.coPlayer.ZeroCondAmount("TutorialHighlightWaiting");
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
		}
	}
}
