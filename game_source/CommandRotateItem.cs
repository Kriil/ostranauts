using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandRotateItem : Command
{
	public CommandRotateItem()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.R);
		this.commandDisplayLabel = "Rotate Item:";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (CrewSim.bRaiseUI)
		{
			return;
		}
		if (CrewSim.Typing)
		{
			return;
		}
		if (base.Down)
		{
			if (CrewSim.inventoryGUI.Selected != null)
			{
				CrewSim.inventoryGUI.RotateCWSelected();
				if (CrewSim.objInstance.goSelPart != null && CrewSim.objInstance.goSelPart.GetComponent<Item>() != null)
				{
					CrewSim.objInstance.goSelPart.GetComponent<Item>().RotateCW();
					CrewSim.inventoryGUI.Selected.CO.Item.fLastRotation = CrewSim.objInstance.goSelPart.GetComponent<Item>().fLastRotation;
				}
				AudioManager.am.PlayAudioEmitter("UIRotate", false, false);
				return;
			}
			if (CrewSim.objInstance.goSelPart == null)
			{
				return;
			}
			if (CrewSim.objInstance.goSelPart.GetComponent<Item>() == null)
			{
				return;
			}
			CrewSim.objInstance.goSelPart.GetComponent<Item>().RotateCW();
			AudioManager.am.PlayAudioEmitter("UIRotate", false, false);
		}
	}
}
