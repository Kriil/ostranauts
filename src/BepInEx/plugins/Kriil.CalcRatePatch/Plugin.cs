using BepInEx;
using HarmonyLib;

namespace Kriil.Ostranauts.CalcRatePatch;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "kriil.ostranauts.calcratepatch";

	public const string PluginName = "CalcRate Patch";

	public const string PluginVersion = "0.1.0";

	private Harmony _harmony;

	private void Awake()
	{
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo("Applied Harmony patches for Interaction.CalcRate.");
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}
