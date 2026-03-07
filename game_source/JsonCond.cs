using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.JsonTypes.Interfaces;
using Ostranauts.Tools.ExtensionMethods;

// Base data definition for a Condition.
// Likely loaded from StreamingAssets/data/conditions and used to create the
// live Condition instances attached to CondOwners.
[Serializable]
public class JsonCond : IVerifiable
{
	// Registry id plus UI-facing name/color/description.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strColor { get; set; }

	public string strDesc { get; set; }

	public string strAnti { get; set; }

	public string strCTImmune { get; set; }

	public bool bResetTimer { get; set; }

	public int nDisplaySelf { get; set; }

	public int nDisplayOther { get; set; }

	public int nDisplayType { get; set; }

	public string strDisplayBonus { get; set; }

	public float fConversionFactor { get; set; }

	public bool bInvert { get; set; }

	public bool bFatal { get; set; }

	public bool bKO { get; set; }

	public bool bAlert { get; set; }

	public bool bRemoveAll { get; set; }

	public bool bPersists { get; set; }

	public bool bNegative { get; set; }

	public bool bRoom { get; set; }

	public bool bRemoveOnLoad { get; set; }

	public bool bQABRefresh { get; set; }

	public bool bCondRuleTrackAlways { get; set; }

	public bool bCondRuleTrackInvert { get; set; }

	public string strTrackCond { get; set; }

	// Follow-up trigger ids and per-step loot hooks.
	// `aNext` should resolve in data/condtrigs; `aPer` likely resolves in data/loot.
	public string[] aNext { get; set; }

	public string[] aPer { get; set; }

	public float fDuration { get; set; }

	public float fClampMax { get; set; }

	public JsonStringIntPair pairFaceSprite { get; set; }

	// Shallow data clone used for template duplication and variant generation.
	public JsonCond Clone()
	{
		JsonCond jsonCond = (JsonCond)base.MemberwiseClone();
		if (jsonCond.aNext != null)
		{
			jsonCond.aNext = (string[])this.aNext.Clone();
		}
		if (jsonCond.aPer != null)
		{
			jsonCond.aPer = (string[])this.aPer.Clone();
		}
		if (jsonCond.pairFaceSprite != null)
		{
			jsonCond.pairFaceSprite = new JsonStringIntPair();
			jsonCond.pairFaceSprite.strName = this.pairFaceSprite.strName;
			jsonCond.pairFaceSprite.nValue = this.pairFaceSprite.nValue;
		}
		return jsonCond;
	}

	// Duplicates a condition while remapping linked ids, then re-registers it in `dictConds`.
	public JsonCond CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		JsonCond jsonCond = this.Clone();
		jsonCond.strName = this.strName.Replace(strFind, strReplace);
		jsonCond.strAnti = JsonCond.CloneDeep(this.strAnti, strReplace, strFind);
		jsonCond.strCTImmune = CondTrigger.CloneDeep(this.strCTImmune, strReplace, strFind);
		if (this.aNext != null)
		{
			for (int i = 0; i < this.aNext.Length; i++)
			{
				jsonCond.aNext[i] = CondTrigger.CloneDeep(this.aNext[i], strReplace, strFind);
			}
		}
		if (this.aPer != null)
		{
			for (int j = 0; j < this.aPer.Length; j++)
			{
				jsonCond.aPer[j] = Loot.CloneDeep(this.aPer[j], strReplace, strFind);
			}
		}
		DataHandler.dictConds[jsonCond.strName] = jsonCond;
		return jsonCond;
	}

	// Helper for remapping one condition id through the shared registry.
	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		JsonCond jsonCond = null;
		if (!DataHandler.dictConds.TryGetValue(strOrigName, out jsonCond))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		JsonCond jsonCond2 = null;
		if (!DataHandler.dictConds.TryGetValue(text, out jsonCond2))
		{
			jsonCond2 = jsonCond.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public override string ToString()
	{
		return this.strName;
	}

	// Validation pass for condition cross-references.
	public IDictionary<string, IEnumerable> GetVerifiables()
	{
		Dictionary<string, IEnumerable> verifiables = new Dictionary<string, IEnumerable>();
		Action<string[], IEnumerable> action = delegate(string[] stringArray, IEnumerable aTypes)
		{
			if (stringArray != null)
			{
				foreach (string text in stringArray)
				{
					if (!string.IsNullOrEmpty(text))
					{
						verifiables.TryAdd(text, null);
					}
				}
			}
		};
		action(this.aPer, new Type[]
		{
			typeof(Loot)
		});
		action(this.aNext, new Type[]
		{
			typeof(CondTrigger)
		});
		if (!string.IsNullOrEmpty(this.strAnti))
		{
			verifiables.TryAdd(this.strAnti, new Type[]
			{
				typeof(JsonCond)
			});
		}
		return verifiables;
	}

	public List<InflectedTokenData> replacementValues = new List<InflectedTokenData>();
}
