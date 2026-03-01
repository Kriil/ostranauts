using BepInEx;
using HarmonyLib;

namespace Kriil.Ostranauts.RoomEffects;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "kriil.ostranauts.roomeffects";

	public const string PluginName = "Room Effects";

	public const string PluginVersion = "0.1.0";

	private Harmony _harmony;

	private void Awake()
	{
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo("Applied Harmony patches for Room Effects.");
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}
