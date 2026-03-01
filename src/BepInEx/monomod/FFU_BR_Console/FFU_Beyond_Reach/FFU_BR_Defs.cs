using System;
using MonoMod;

namespace FFU_Beyond_Reach
{
	[MonoModIgnore]
	// Shared configuration and constant table for the FFU Beyond Reach mod suite.
	// This is the central BepInEx-backed API surface that exposes loader flags,
	// changesMap command names, and module-specific gameplay/QoL toggles.
	public class FFU_BR_Defs
	{
		public static FFU_BR_Defs.SyncLogs SyncLogging;
		public static FFU_BR_Defs.ActLogs ActLogging;
		public static bool JsonLogging;
		public static bool DynamicRandomRange;
		public static int MaxLogTextSize;
		public static bool ModSyncLoading;
		public static bool EnableCodeFixes;
		public static bool ModifyUpperLimit;
		public static float BonusUpperLimit;
		public static float SuitOxygenNotify;
		public static float SuitPowerNotify;
		public static bool ShowEachO2Battery;
		public static bool StrictInvSorting;
		public static bool AltTempEnabled;
		public static string AltTempSymbol;
		public static float AltTempMult;
		public static float AltTempShift;
		public static bool TowBraceAllowsKeep;
		public static bool OrgInventoryMode;
		public static float[] OrgInventoryTweaks;
		public static bool BetterInvTransfer;
		public static bool QuickBarPinning;
		public static float[] QuickBarTweaks;
		public static bool NoSkillTraitCost;
		public static bool AllowSuperChars;
		public static float SuperCharMultiplier;
		public static string[] SuperCharacters;
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
