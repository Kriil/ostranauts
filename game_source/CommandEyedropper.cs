using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandEyedropper : Command
{
	public CommandEyedropper()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.Z);
		this.commandDisplayLabel = "Eyedropper:";
	}
}
