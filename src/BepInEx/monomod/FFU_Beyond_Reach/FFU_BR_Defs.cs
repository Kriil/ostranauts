using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace FFU_Beyond_Reach
{
	// Shared configuration and constant table for the FFU Beyond Reach mod suite.
	// This is the central BepInEx-backed API surface that exposes loader flags,
	// changesMap command names, and module-specific gameplay/QoL toggles.
	public class FFU_BR_Defs
	{
		// Loads or creates the shared BepInEx config file for the full FFU_BR suite.
		// Data mods and runtime patches both read these values, so this acts as the
		// central feature toggle point before patched systems begin executing.
		public static void InitConfig()
		{
			FFU_BR_Defs.ModDefs = new ConfigFile(Path.Combine(Paths.ConfigPath, "FFU_Beyond_Reach.cfg"), true);
			Debug.Log(FFU_BR_Defs.ModName + " v" + FFU_BR_Defs.ModVersion);
			Debug.Log("Loading Mod Configuration...");
			FFU_BR_Defs.SyncLogging = FFU_BR_Defs.ModDefs.Bind<FFU_BR_Defs.SyncLogs>("ConfigSettings", "SyncLogging", FFU_BR_Defs.SyncLogging, "Defines what changes will be shown in the log during sync loading.").Value;
			FFU_BR_Defs.ActLogging = FFU_BR_Defs.ModDefs.Bind<FFU_BR_Defs.ActLogs>("ConfigSettings", "ActLogging", FFU_BR_Defs.ActLogging, "Defines what activity will be shown in the log during gameplay/runtime.").Value;
			FFU_BR_Defs.JsonLogging = FFU_BR_Defs.ModDefs.Bind<bool>("ConfigSettings", "JsonLogging", FFU_BR_Defs.JsonLogging, "Defines if JSON parsing errors will be shown in the log during loading.").Value;
			FFU_BR_Defs.DynamicRandomRange = FFU_BR_Defs.ModDefs.Bind<bool>("ConfigSettings", "DynamicRandomRange", FFU_BR_Defs.DynamicRandomRange, "By default loot random range is limited to 1f, thus preventing use of loot tables, if total sum of their chances goes beyond 1f. This feature allows to increase max possible random range beyond 1f, to the total sum of all chances in the loot table.").Value;
			FFU_BR_Defs.MaxLogTextSize = FFU_BR_Defs.ModDefs.Bind<int>("ConfigSettings", "MaxLogTextSize", FFU_BR_Defs.MaxLogTextSize, "Defines the max length of the text in the console. May impact performance.").Value;
			FFU_BR_Defs.ModSyncLoading = FFU_BR_Defs.ModDefs.Bind<bool>("ConfigSettings", "ModSyncLoading", FFU_BR_Defs.ModSyncLoading, "Enables smart loading of modified COs and synchronizing of existing CO saved data with updated CO templates, if they are mapped in the mod info file.").Value;
			FFU_BR_Defs.EnableCodeFixes = FFU_BR_Defs.ModDefs.Bind<bool>("ConfigSettings", "EnableCodeFixes", FFU_BR_Defs.EnableCodeFixes, "Enables various vanilla code fixes. Added for cross-version compatibility. If causes any issues, please disable. Option might have no effect in future versions.").Value;
			FFU_BR_Defs.ModifyUpperLimit = FFU_BR_Defs.ModDefs.Bind<bool>("GameplaySettings", "ModifyUpperLimit", FFU_BR_Defs.ModifyUpperLimit, "Allows to change skill and trait modifier upper limit value.").Value;
			FFU_BR_Defs.BonusUpperLimit = FFU_BR_Defs.ModDefs.Bind<float>("GameplaySettings", "BonusUpperLimit", FFU_BR_Defs.BonusUpperLimit, "Defines the upper limit for skill and trait modifier. Original value is 10.").Value;
			FFU_BR_Defs.SuitOxygenNotify = FFU_BR_Defs.ModDefs.Bind<float>("GameplaySettings", "SuitOxygenNotify", FFU_BR_Defs.SuitOxygenNotify, "Specifies the oxygen level threshold (as a percentage) for the gauge of a sealed/airtight suit. When the oxygen level falls below this threshold, the wearer will receive a notification (via occasional beeps) about oxygen usage. If set to 0, no notification will be given at any time.").Value;
			FFU_BR_Defs.SuitPowerNotify = FFU_BR_Defs.ModDefs.Bind<float>("GameplaySettings", "SuitPowerNotify", FFU_BR_Defs.SuitPowerNotify, "Specifies the power level threshold (as a percentage) for the gauge of a sealed/airtight suit. When the power level falls below this threshold, the wearer will receive a notification (via frequent beeps) about power usage. If set to 0, no notification will be given at any time.").Value;
			FFU_BR_Defs.ShowEachO2Battery = FFU_BR_Defs.ModDefs.Bind<bool>("GameplaySettings", "ShowEachO2Battery", FFU_BR_Defs.ShowEachO2Battery, "Defines whether to show average percentage across all O2/Batteries or calculate each O2/Battery independently and summarize their percentages. Affects how soon notifications will begin.").Value;
			FFU_BR_Defs.StrictInvSorting = FFU_BR_Defs.ModDefs.Bind<bool>("GameplaySettings", "StrictInvSorting", FFU_BR_Defs.StrictInvSorting, "Enables custom, order-based inventory windows sorting that enforces strict UI rendering order.").Value;
			Debug.Log(string.Format("GameplaySettings => ModifyUpperLimit: {0}", FFU_BR_Defs.ModifyUpperLimit));
			Debug.Log(string.Format("GameplaySettings => BonusUpperLimit: {0}", FFU_BR_Defs.BonusUpperLimit));
			Debug.Log(string.Format("GameplaySettings => SuitOxygenNotify: {0}%", FFU_BR_Defs.SuitOxygenNotify));
			Debug.Log(string.Format("GameplaySettings => SuitPowerNotify: {0}%", FFU_BR_Defs.SuitPowerNotify));
			Debug.Log(string.Format("GameplaySettings => ShowEachO2Battery: {0}", FFU_BR_Defs.ShowEachO2Battery));
			Debug.Log(string.Format("GameplaySettings => StrictInvSorting: {0}", FFU_BR_Defs.StrictInvSorting));
			FFU_BR_Defs.AltTempEnabled = FFU_BR_Defs.ModDefs.Bind<bool>("QualitySettings", "AltTempEnabled", FFU_BR_Defs.AltTempEnabled, "Allows to show temperature in alternative measure beside Kelvin value.").Value;
			FFU_BR_Defs.AltTempSymbol = FFU_BR_Defs.ModDefs.Bind<string>("QualitySettings", "AltTempSymbol", FFU_BR_Defs.AltTempSymbol, "What symbol will represent alternative temperature measure.").Value;
			FFU_BR_Defs.AltTempMult = FFU_BR_Defs.ModDefs.Bind<float>("QualitySettings", "AltTempMult", FFU_BR_Defs.AltTempMult, "Alternative temperature multiplier for conversion from Kelvin.").Value;
			FFU_BR_Defs.AltTempShift = FFU_BR_Defs.ModDefs.Bind<float>("QualitySettings", "AltTempShift", FFU_BR_Defs.AltTempShift, "Alternative temperature value shift for conversion from Kelvin.").Value;
			FFU_BR_Defs.TowBraceAllowsKeep = FFU_BR_Defs.ModDefs.Bind<bool>("QualitySettings", "TowBraceAllowsKeep", FFU_BR_Defs.TowBraceAllowsKeep, "Allows to use station keeping command, while tow braced to another vessel.").Value;
			FFU_BR_Defs.OrgInventoryMode = FFU_BR_Defs.ModDefs.Bind<bool>("QualitySettings", "OrgInventoryMode", FFU_BR_Defs.OrgInventoryMode, "Changes inventory layout and makes smart use of available space.").Value;
			string value = FFU_BR_Defs.ModDefs.Bind<string>("QualitySettings", "OrgInventoryTweaks", string.Join("|", Array.ConvertAll<float, string>(FFU_BR_Defs.OrgInventoryTweaks, (float n) => n.ToString())), "Inventory offsets for tweaking: Top Offset, Bottom Limit, Horizontal Padding, Vertical Padding.").Value;
			bool flag = value.Split(new char[]
			{
				'|'
			}).Length == 4;
			if (flag)
			{
				FFU_BR_Defs.OrgInventoryTweaks = Array.ConvertAll<string, float>(value.Split(new char[]
				{
					'|'
				}), delegate(string x)
				{
					float num;
					return float.TryParse(x, out num) ? num : 0f;
				});
			}
			FFU_BR_Defs.BetterInvTransfer = FFU_BR_Defs.ModDefs.Bind<bool>("QualitySettings", "BetterInvTransfer", FFU_BR_Defs.BetterInvTransfer, "Changes behavior of shift-click item transferring in inventory. Items will be auto-transferred to the last inventory window, where player has placed the item manually. Last inventory window is forgotten, when inventory is closed.").Value;
			FFU_BR_Defs.QuickBarPinning = FFU_BR_Defs.ModDefs.Bind<bool>("QualitySettings", "QuickBarPinning", FFU_BR_Defs.QuickBarPinning, "Allows to permanently lock the interactions quick bar, where you desire.").Value;
			string value2 = FFU_BR_Defs.ModDefs.Bind<string>("QualitySettings", "QuickBarTweaks", string.Join("|", Array.ConvertAll<float, string>(FFU_BR_Defs.QuickBarTweaks, (float n) => n.ToString())), "Quick Bar offsets for tweaking: Horizontal, Vertical, Expanded.").Value;
			bool flag2 = value2.Split(new char[]
			{
				'|'
			}).Length == 3;
			if (flag2)
			{
				FFU_BR_Defs.QuickBarTweaks = Array.ConvertAll<string, float>(value2.Split(new char[]
				{
					'|'
				}), delegate(string x)
				{
					float num;
					return float.TryParse(x, out num) ? num : 0f;
				});
			}
			Debug.Log(string.Format("QualitySettings => AltTempEnabled: {0}", FFU_BR_Defs.AltTempEnabled));
			Debug.Log(string.Format("QualitySettings => AltTempSymbol: {0}", FFU_BR_Defs.AltTempEnabled));
			Debug.Log(string.Format("QualitySettings => AltTempMult: {0}", FFU_BR_Defs.AltTempMult));
			Debug.Log(string.Format("QualitySettings => AltTempShift: {0}", FFU_BR_Defs.AltTempShift));
			Debug.Log(string.Format("QualitySettings => TowBraceAllowsKeep: {0}", FFU_BR_Defs.TowBraceAllowsKeep));
			Debug.Log(string.Format("QualitySettings => OrgInventoryMode: {0}", FFU_BR_Defs.OrgInventoryMode));
			Debug.Log("QualitySettings => OrgInventoryTweaks: " + string.Join(", ", Array.ConvertAll<float, string>(FFU_BR_Defs.OrgInventoryTweaks, (float x) => x.ToString())));
			Debug.Log(string.Format("QualitySettings => BetterInvTransfer: {0}", FFU_BR_Defs.BetterInvTransfer));
			Debug.Log(string.Format("QualitySettings => QuickBarPinning: {0}", FFU_BR_Defs.QuickBarPinning));
			Debug.Log("QualitySettings => QuickBarTweaks: " + string.Join(", ", Array.ConvertAll<float, string>(FFU_BR_Defs.QuickBarTweaks, (float x) => x.ToString())));
			FFU_BR_Defs.NoSkillTraitCost = FFU_BR_Defs.ModDefs.Bind<bool>("SuperSettings", "NoSkillTraitCost", FFU_BR_Defs.NoSkillTraitCost, "Makes all trait and/or skill changes free, regardless of their cost.").Value;
			FFU_BR_Defs.AllowSuperChars = FFU_BR_Defs.ModDefs.Bind<bool>("SuperSettings", "AllowSuperChars", FFU_BR_Defs.AllowSuperChars, "Allows existence of super characters with extreme performance bonuses.").Value;
			FFU_BR_Defs.SuperCharMultiplier = FFU_BR_Defs.ModDefs.Bind<float>("SuperSettings", "SuperCharMultiplier", FFU_BR_Defs.SuperCharMultiplier, "Defines the bonus multiplier for super characters performance.").Value;
			string value3 = FFU_BR_Defs.ModDefs.Bind<string>("SuperSettings", "SuperCharacters", string.Join("|", FFU_BR_Defs.SuperCharacters), "Lower-case list of super characters that will receive boost on name basis.").Value;
			bool flag3 = !string.IsNullOrEmpty(value3);
			if (flag3)
			{
				FFU_BR_Defs.SuperCharacters = value3.Split(new char[]
				{
					'|'
				});
			}
			Debug.Log(string.Format("SuperSettings => NoSkillTraitCost: {0}", FFU_BR_Defs.NoSkillTraitCost));
			Debug.Log(string.Format("SuperSettings => AllowSuperChars: {0}", FFU_BR_Defs.AllowSuperChars));
			Debug.Log(string.Format("SuperSettings => SuperCharMultiplier: {0}", FFU_BR_Defs.SuperCharMultiplier));
			Debug.Log("SuperSettings => SuperCharacters: " + string.Join(", ", FFU_BR_Defs.SuperCharacters));
		}
		static FFU_BR_Defs()
		{
			float[] array = new float[4];
			array[0] = 0.5f;
			array[1] = -35f;
			FFU_BR_Defs.OrgInventoryTweaks = array;
			FFU_BR_Defs.BetterInvTransfer = true;
			FFU_BR_Defs.QuickBarPinning = false;
			FFU_BR_Defs.QuickBarTweaks = new float[]
			{
				-495f,
				340f,
				1f
			};
			FFU_BR_Defs.NoSkillTraitCost = false;
			FFU_BR_Defs.AllowSuperChars = false;
			FFU_BR_Defs.SuperCharMultiplier = 10f;
			FFU_BR_Defs.SuperCharacters = new string[]
			{
				"Exact Char Name One",
				"Exact Char Name Two"
			};
		}
		public static readonly string ModName = "Fight For Universe: Beyond Reach";
		public static readonly string ModVersion = "0.5.5.5";
		private static ConfigFile ModDefs = null;
		public static FFU_BR_Defs.SyncLogs SyncLogging = FFU_BR_Defs.SyncLogs.None;
		public static FFU_BR_Defs.ActLogs ActLogging = FFU_BR_Defs.ActLogs.None;
		public static bool JsonLogging = false;
		public static bool DynamicRandomRange = true;
		public static int MaxLogTextSize = 16382;
		public static bool ModSyncLoading = true;
		public static bool EnableCodeFixes = true;
		public static bool ModifyUpperLimit = false;
		public static float BonusUpperLimit = 1000f;
		public static float SuitOxygenNotify = 10f;
		public static float SuitPowerNotify = 15f;
		public static bool ShowEachO2Battery = true;
		public static bool StrictInvSorting = true;
		public static bool AltTempEnabled = true;
		public static string AltTempSymbol = "C";
		public static float AltTempMult = 1f;
		public static float AltTempShift = -273.15f;
		public static bool TowBraceAllowsKeep = true;
		public static bool OrgInventoryMode = false;
		public static float[] OrgInventoryTweaks;
		public static bool BetterInvTransfer;
		public static bool QuickBarPinning;
		public static float[] QuickBarTweaks;
		public static bool NoSkillTraitCost;
		public static bool AllowSuperChars;
		public static float SuperCharMultiplier;
		public static string[] SuperCharacters;
		public const string SYM_DIV = "|";
		public const string SYM_EQU = "=";
		public const string SYM_IGN = "*";
		public const string SYM_INV = "!";
		public const string CMD_SWITCH_SLT = "Switch_Slotted";
		public const string CMD_REC_MISSING = "Recover_Missing";
		public const string CMD_CONDS_SYN = "Sync_Conditions";
		public const string CMD_CONDS_UPD = "Update_Conditions";
		public const string CMD_EFFECT_SLT = "Sync_Slot_Effects";
		public const string CMD_EFFECT_INV = "Sync_Inv_Effects";
		public const string FLAG_INVERSE = "*IsInverse*";
		public const string OPT_DEL = "~";
		public const string OPT_MOD = "*";
		public const string OPT_REM = "-";
		public enum SyncLogs
		{
			None,
			ModChanges,
			DeepCopy,
			ModdedDump,
			ExtendedDump,
			ContentDump,
			SourceDump
		}
		public enum ActLogs
		{
			None,
			Interactions,
			Runtime
		}
	}
}
