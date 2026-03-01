using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.JsonTypes.Interfaces;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

// Base data definition for a CondOwner archetype.
// Likely loaded from StreamingAssets/data/condowners, then referenced by items,
// ships, rooms, or overlays that need condition-bearing behavior.
// Multiple COOverlay records can point at one CondOwner definition at runtime.
[Serializable]
public class JsonCondOwner : IVerifiable
{
	// `strName` is the registry key; friendly text feeds UI labels and tooltips.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strNameShort { get; set; }

	public string strDesc { get; set; }

	// Links this record back to another CondOwner or item definition when reused.
	public string strCODef { get; set; }

	public string strItemDef { get; set; }

	public int inventoryWidth { get; set; }

	public int inventoryHeight { get; set; }

	public string strType { get; set; }

	// Loot and CondTrigger hooks likely reference data/loot and data/condtrigs.
	public string strLoot { get; set; }

	public string strContainerCT { get; set; }

	public int nStackLimit { get; set; }

	public int nContainerHeight { get; set; }

	public int nContainerWidth { get; set; }

	// Available actions and default conditions applied when this owner is spawned.
	// Interaction ids should resolve in data/interactions; condition ids in data/conditions.
	public string[] aInteractions { get; set; }

	public string[] aStartingConds { get; set; }

	public string[] aStartingCondRules { get; set; }

	public bool bSaveMessageLog { get; set; }

	public bool bSlotLocked { get; set; }

	public string[] mapPoints { get; set; }

	public string[] aUpdateCommands { get; set; }

	public string[] mapGUIPropMaps { get; set; }

	public string strPortraitImg { get; set; }

	public string strAudioEmitter { get; set; }

	public string jsonPI { get; set; }

	public string[] aSlotsWeHave { get; set; }

	public string[] mapSlotEffects { get; set; }

	public string[] mapChargeProfiles { get; set; }

	public string[] mapAltItemDefs { get; set; }

	public string[] mapAltSlotImgs { get; set; }

	public string[] aComponents { get; set; }

	public string[] aTickers { get; set; }

	// Likely used while loading job/career content to append action lists without replacing the whole map.
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

	// Returns a safe empty list when the keyed job action bucket is absent.
	public List<string> GetJobActions(string strKey)
	{
		if (strKey == null || this.mapJobActions == null || !this.mapJobActions.ContainsKey(strKey))
		{
			return new List<string>();
		}
		return this.mapJobActions[strKey];
	}

	public override string ToString()
	{
		return this.strName;
	}

	// Validation pass used by the data loader to collect cross-file references before gameplay starts.
	public IDictionary<string, IEnumerable> GetVerifiables()
	{
		Dictionary<string, IEnumerable> dictionary = new Dictionary<string, IEnumerable>();
		if (this.strContainerCT != null)
		{
			dictionary.TryAdd(this.strContainerCT, new Type[]
			{
				typeof(CondTrigger)
			});
		}
		if (this.strItemDef != null)
		{
			dictionary.TryAdd(this.strItemDef, new Type[]
			{
				typeof(JsonItemDef)
			});
		}
		if (this.strLoot != null)
		{
			dictionary.TryAdd(this.strLoot, new Type[]
			{
				typeof(Loot)
			});
		}
		if (this.aInteractions != null)
		{
			foreach (string text in this.aInteractions)
			{
				if (!string.IsNullOrEmpty(text))
				{
					dictionary.TryAdd(text, new Type[]
					{
						typeof(JsonInteraction)
					});
				}
			}
		}
		if (this.aStartingConds != null && this.aStartingConds.Length > 0)
		{
			foreach (string text2 in this.aStartingConds)
			{
				if (!string.IsNullOrEmpty(text2))
				{
					string[] array = text2.Split(new char[]
					{
						'='
					});
					if (array != null && array.Length > 0)
					{
						if (array[0].StartsWith("-"))
						{
							array[0] = array[0].Substring(1);
						}
						dictionary.TryAdd(array[0], new Type[]
						{
							typeof(JsonCond)
						});
					}
				}
			}
		}
		if (this.aStartingCondRules != null && this.aStartingCondRules.Length > 0)
		{
			foreach (string text3 in this.aStartingCondRules)
			{
				if (!string.IsNullOrEmpty(text3))
				{
					string[] array2 = text3.Split(new char[]
					{
						'='
					});
					dictionary.TryAdd(array2[0], new Type[]
					{
						typeof(CondRule)
					});
				}
			}
		}
		if (this.aTickers != null && this.aTickers.Length > 0)
		{
			foreach (string text4 in this.aTickers)
			{
				if (!string.IsNullOrEmpty(text4))
				{
					dictionary.TryAdd(text4, new Type[]
					{
						typeof(JsonTicker)
					});
				}
			}
		}
		return dictionary;
	}

	public Dictionary<string, Vector3> dictSlotsLayout;

	private Dictionary<string, List<string>> mapJobActions;
}
