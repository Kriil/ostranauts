using System;

namespace Ostranauts.Blueprints;

public sealed class BlueprintCommand : DevCommand
{
	public BlueprintCommand()
	{
		keyword = "blueprint";
		DevConsole.commands[keyword] = this;
	}

	public override void Execute(string input)
	{
		try
		{
			BlueprintRuntime.StartSelectionMode();
			DevConsole.Output("<color=#41D0DDFF>>> " + input + "</color>");
			DevConsole.Output("Blueprint mode active. Drag-select installables, right-click to cancel, R to rotate the placement preview.");
		}
		catch (Exception ex)
		{
			Plugin.LogException("blueprint command", ex);
			DevConsole.Output("Blueprint mode failed to start. Check the log.");
		}
	}
}
