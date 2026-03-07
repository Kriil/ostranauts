using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CommandToogleTooltipDetail : Command
{
	public CommandToogleTooltipDetail()
	{
		this.defaultCombo = new List<KeyCode>
		{
			KeyCode.M
		};
		this.commandDisplayLabel = "Toggle Tooltip detail";
	}

	public override void Execute()
	{
		if (base.Down)
		{
			GUIActionKeySelector.OnKeyDown.Invoke(this.defaultCombo.FirstOrDefault<KeyCode>());
		}
	}
}
