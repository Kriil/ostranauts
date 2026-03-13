namespace Ostranauts.Blueprints;

public static class BlueprintCommand
{
	public static string Execute(string input)
	{
		BlueprintRuntime.StartSelectionMode();
		return "\nBlueprint mode active. Drag-select installables, right-click to cancel, R to rotate the placement preview.";
	}
}
