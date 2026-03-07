using System;
using System.Collections.Generic;

// Base data definition for a COOverlay variant.
// Likely loaded from StreamingAssets/data/cooverlays and used to patch a live
// CondOwner's display text, art, slot rules, and interaction set without
// replacing the underlying CondOwner definition.
[Serializable]
public class JsonCOOverlay
{
	// Overlay id plus UI-facing display text.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strNameShort { get; set; }

	public string strDesc { get; set; }

	public string strImg { get; set; }

	public string strImgDamaged { get; set; }

	public string strDmgColor { get; set; }

	public string strImgNorm { get; set; }

	public string strPortraitImg { get; set; }

	// Base CondOwner id and loot script used when this variant is applied.
	// `strCOBase` should resolve in data/condowners; `strCondLoot` likely resolves in data/loot.
	public string strCOBase { get; set; }

	public string strCondLoot { get; set; }

	// Flat key/value arrays that are unpacked into runtime dictionaries on demand.
	public string[] mapModeSwitches { get; set; }

	public string[] aDestSwaps { get; set; }

	public string[] mapSlotEffects { get; set; }

	public string[] mapAltItemDefs { get; set; }

	public string[] mapAltSlotImgs { get; set; }

	public string[] mapGUIPropMaps { get; set; }

	public string[] aInteractionsReplace { get; set; }

	public JsonItemAnimation objAnimation { get; set; }

	// Likely used by loader post-processing to append job-specific actions for this overlay.
	public void AddJobAction(string strKey, string strValue)
	{
		if (strKey == null || strValue == null)
		{
			return;
		}
		if (this.mapJobActions == null)
		{
			this.mapJobActions = new Dictionary<string, List<string>>();
		}
		if (!this.mapJobActions.ContainsKey(strKey))
		{
			this.mapJobActions[strKey] = new List<string>();
		}
		this.mapJobActions[strKey].Add(strValue);
	}

	// Returns a safe empty list when the overlay has no job-specific action overrides.
	public List<string> GetJobActions(string strKey)
	{
		if (strKey == null || this.mapJobActions == null || !this.mapJobActions.ContainsKey(strKey))
		{
			return new List<string>();
		}
		return this.mapJobActions[strKey];
	}

	// Resolves a target CO id to the overlay-specific replacement variant.
	public string GetModeSwitch(string strTargetCO)
	{
		if (strTargetCO == null)
		{
			return null;
		}
		if (this.mapMS == null)
		{
			this.mapMS = new Dictionary<string, string>();
			if (this.mapModeSwitches != null)
			{
				for (int i = 0; i < this.mapModeSwitches.Length - 1; i += 2)
				{
					if (this.mapModeSwitches[i] == null || this.mapModeSwitches[i + 1] == null)
					{
						break;
					}
					this.mapMS[this.mapModeSwitches[i]] = this.mapModeSwitches[i + 1];
				}
			}
		}
		string result = null;
		if (this.mapMS.TryGetValue(strTargetCO, out result))
		{
			return result;
		}
		return result;
	}

	// Lazily builds the interaction replacement map from the serialized string array.
	public Dictionary<string, string> InteractionReplacements
	{
		get
		{
			if (this.mapIAReplaces == null)
			{
				this.mapIAReplaces = new Dictionary<string, string>();
				if (this.aInteractionsReplace != null)
				{
					for (int i = 0; i < this.aInteractionsReplace.Length - 1; i += 2)
					{
						if (this.aInteractionsReplace[i] == null || this.aInteractionsReplace[i + 1] == null)
						{
							break;
						}
						this.mapIAReplaces[this.aInteractionsReplace[i]] = this.aInteractionsReplace[i + 1];
					}
				}
			}
			return this.mapIAReplaces;
		}
	}

	public override string ToString()
	{
		return this.strName;
	}

	private Dictionary<string, string> mapMS;

	private Dictionary<string, string> mapCMD;

	private Dictionary<string, string> mapIAReplaces;

	private Dictionary<string, List<string>> mapJobActions;
}
