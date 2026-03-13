using HarmonyLib;

namespace Ostranauts.Blueprints;

[HarmonyPatch(typeof(ConsoleResolver), "ResolveString")]
public static class Patch_ConsoleResolver_BlueprintsResolve
{
	[HarmonyPrefix]
	private static bool Prefix(ref string strInput, ref bool __result)
	{
		if (strInput == null)
		{
			return true;
		}

		string trimmed = strInput.Trim();
		if (trimmed.Length == 0)
		{
			return true;
		}

		string[] parts = trimmed.Split(' ');
		if (parts.Length == 0 || parts[0].ToLower() != "blueprint")
		{
			return true;
		}

		try
		{
			strInput += BlueprintCommand.Execute(strInput);
		}
		catch (System.Exception ex)
		{
			Plugin.LogException("blueprint console command", ex);
			strInput += "\nBlueprint mode failed to start. Check the log.";
		}

		__result = true;
		return false;
	}
}

[HarmonyPatch(typeof(ConsoleResolver), "KeywordHelp")]
public static class Patch_ConsoleResolver_BlueprintsHelp
{
	[HarmonyPostfix]
	private static void Postfix(ref string strInput, string[] strings)
	{
		if (strings == null || strings.Length == 0)
		{
			return;
		}

		string keyword = strings[0].ToLower();
		if (keyword != "help")
		{
			return;
		}

		if (strings.Length == 1)
		{
			strInput += "\nblueprint";
			return;
		}

		if (strings[1].ToLower() != "blueprint")
		{
			return;
		}

		strInput += "\nblueprint starts blueprint mode.";
		strInput += "\nDrag-select installables to queue uninstall tasks and capture a temporary blueprint.";
		strInput += "\nLeft-click places the blueprint as install jobs, right-click cancels, R rotates the preview.";
		strInput += "\n";
	}
}
