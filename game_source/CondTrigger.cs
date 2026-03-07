using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.JsonTypes.Interfaces;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

// Condition trigger/test definition plus runtime helper methods.
// In practice this looks like the game's generic rule primitive for checking
// condition requirements and optionally applying condition/loot effects.
public class CondTrigger : IVerifiable
{
	// Default ctor used by deserialization.
	public CondTrigger()
	{
		this.Init();
	}

	// Convenience ctor for simple "apply this condition/count" triggers.
	public CondTrigger(string strName, string strCondName, float fCount)
	{
		this.Init();
		this.strName = strName;
		this.strCondName = strCondName;
		this._fCount = fCount;
	}

	// Convenience ctor for ad-hoc trigger building with req/forbid and nested trigger lists.
	public CondTrigger(string strName, string[] aReqs, string[] aForbids, string[] aTriggers, string[] aTriggersForbid)
	{
		this.Init();
		this.strName = strName;
		if (aReqs != null)
		{
			this.aReqs = aReqs;
		}
		if (aForbids != null)
		{
			this.aForbids = aForbids;
		}
		bool flag = false;
		if (aTriggers != null && aTriggers.Length > 0)
		{
			this.aTriggers = aTriggers;
			flag = true;
		}
		if (aTriggersForbid != null && aTriggersForbid.Length > 0)
		{
			this.aTriggersForbid = aTriggersForbid;
			flag = true;
		}
		if (flag)
		{
			this.PostInit();
		}
	}

	public string strName { get; set; }

	public string strCondName { get; set; }

	public float fChance
	{
		get
		{
			return this._fChance;
		}
		set
		{
			if (value != this._fChance)
			{
				this._valuesWereChanged = true;
			}
			this._fChance = value;
		}
	}

	public float fCount
	{
		get
		{
			return this._fCount;
		}
		set
		{
			if (value != this._fCount)
			{
				this._valuesWereChanged = true;
			}
			this._fCount = value;
		}
	}

	public string[] aReqs
	{
		get
		{
			return this._aReqs;
		}
		set
		{
			this._valuesWereChanged = true;
			this._aReqs = value;
		}
	}

	public string[] aForbids
	{
		get
		{
			return this._aForbids;
		}
		set
		{
			this._valuesWereChanged = true;
			this._aForbids = value;
		}
	}

	public string[] aTriggers
	{
		get
		{
			return this._aTriggers;
		}
		set
		{
			this._valuesWereChanged = true;
			this._aTriggers = value;
		}
	}

	public string[] aTriggersForbid
	{
		get
		{
			return this._aTriggersForbid;
		}
		set
		{
			this._valuesWereChanged = true;
			this._aTriggersForbid = value;
		}
	}

	public string strHigherCond { get; set; }

	public string[] aLowerConds
	{
		get
		{
			return this._aLowerConds;
		}
		set
		{
			this._valuesWereChanged = true;
			this._aLowerConds = value;
		}
	}

	public bool ValuesWereChanged
	{
		get
		{
			return this._valuesWereChanged;
		}
		set
		{
			this._valuesWereChanged = value;
		}
	}

	public bool RequiresHumans
	{
		get
		{
			bool? requiresHumans = this._requiresHumans;
			if (requiresHumans != null)
			{
				bool? requiresHumans2 = this._requiresHumans;
				return requiresHumans2.Value;
			}
			this._requiresHumans = new bool?(this.CheckRequiresHumans());
			bool? requiresHumans3 = this._requiresHumans;
			return requiresHumans3.Value;
		}
	}

	// Initializes default arrays and shared fallback relationship state.
	private void Init()
	{
		this._fChance = 1f;
		this.aReqs = CondTrigger._aDefault;
		this.aForbids = CondTrigger._aDefault;
		this.aTriggers = CondTrigger._aDefault;
		this.aTriggersForbid = CondTrigger._aDefault;
		this.aLowerConds = CondTrigger._aDefault;
		this._valuesWereChanged = false;
		if (CondTrigger.relStranger == null)
		{
			CondTrigger.relStranger = new Relationship();
			CondTrigger.relStranger.aRelationships = new List<string>
			{
				"RELStranger"
			};
		}
	}

	// Resolves nested trigger ids into cached trigger dictionaries after load.
	public void PostInit()
	{
		if (this.aTriggers != null)
		{
			foreach (string strTrig in this.aTriggers)
			{
				this.SetTrigger(strTrig, CondTrigger.CTDict.Triggers);
			}
		}
		if (this.aTriggersForbid != null)
		{
			foreach (string strTrig2 in this.aTriggersForbid)
			{
				this.SetTrigger(strTrig2, CondTrigger.CTDict.Forbids);
			}
		}
		this._requiresHumans = new bool?(this.CheckRequiresHumans());
	}

	// Likely a cached heuristic used by AI/social systems to skip expensive checks.
	private bool CheckRequiresHumans()
	{
		this.nRecursion++;
		if (this.nRecursion > 3)
		{
			Debug.LogWarning("Possible recursion found in CT: " + this.strName);
			return false;
		}
		if (this.aReqs.Contains("IsHuman") || this.aReqs.Contains("IsPlayer"))
		{
			return true;
		}
		if (this.aTriggersConds != null)
		{
			foreach (CondTrigger condTrigger in this.aTriggersConds.Values)
			{
				if (condTrigger.RequiresHumans)
				{
					return true;
				}
			}
		}
		if (this.aTriggersForbidConds != null)
		{
			foreach (CondTrigger condTrigger2 in this.aTriggersForbidConds.Values)
			{
				if (condTrigger2.RequiresHumans)
				{
					return false;
				}
			}
		}
		return false;
	}

	// Returns a detached copy of the trigger definition.
	public CondTrigger Clone()
	{
		return new CondTrigger
		{
			strName = this.strName,
			strCondName = this.strCondName,
			_fChance = this._fChance,
			_fCount = this._fCount,
			bAND = this.bAND,
			aReqs = this.aReqs,
			aForbids = this.aForbids,
			aTriggers = this.aTriggers,
			aTriggersConds = this.aTriggersConds,
			aTriggersForbid = this.aTriggersForbid,
			aTriggersForbidConds = this.aTriggersForbidConds,
			_requiresHumans = this._requiresHumans,
			strFailReason = this.strFailReason,
			strFailReasonLast = this.strFailReasonLast,
			strHigherCond = this.strHigherCond,
			aLowerConds = this.aLowerConds,
			nFilterMultiple = this.nFilterMultiple,
			_isBlank = this._isBlank,
			_valuesWereChanged = false
		};
	}

	public CondTrigger CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		CondTrigger condTrigger = this.Clone();
		condTrigger.strName = this.strName.Replace(strFind, strReplace);
		condTrigger.strCondName = JsonCond.CloneDeep(this.strCondName, strReplace, strFind);
		if (this.aForbids != null)
		{
			condTrigger.aForbids = new string[this.aForbids.Length];
			for (int i = 0; i < this.aForbids.Length; i++)
			{
				condTrigger.aForbids[i] = JsonCond.CloneDeep(this.aForbids[i], strReplace, strFind);
				condTrigger._isBlank = false;
			}
		}
		if (this.aReqs != null)
		{
			condTrigger.aReqs = new string[this.aReqs.Length];
			for (int j = 0; j < this.aReqs.Length; j++)
			{
				condTrigger.aReqs[j] = JsonCond.CloneDeep(this.aReqs[j], strReplace, strFind);
				condTrigger._isBlank = false;
			}
		}
		if (this.aTriggers != null)
		{
			condTrigger.aTriggers = new string[this.aTriggers.Length];
			for (int k = 0; k < this.aTriggers.Length; k++)
			{
				condTrigger.aTriggers[k] = CondTrigger.CloneDeep(this.aTriggers[k], strReplace, strFind);
				condTrigger._isBlank = false;
			}
		}
		if (this.aTriggersForbid != null)
		{
			condTrigger.aTriggersForbid = new string[this.aTriggersForbid.Length];
			for (int l = 0; l < this.aTriggersForbid.Length; l++)
			{
				condTrigger.aTriggersForbid[l] = CondTrigger.CloneDeep(this.aTriggersForbid[l], strReplace, strFind);
				condTrigger._isBlank = false;
			}
		}
		if (this.aLowerConds != null)
		{
			condTrigger.aLowerConds = new string[this.aLowerConds.Length];
			for (int m = 0; m < this.aLowerConds.Length; m++)
			{
				condTrigger.aLowerConds[m] = CondTrigger.CloneDeep(this.aLowerConds[m], strReplace, strFind);
				condTrigger._isBlank = false;
			}
		}
		condTrigger.PostInit();
		DataHandler.dictCTs[condTrigger.strName] = condTrigger;
		return condTrigger;
	}

	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		CondTrigger condTrigger = null;
		if (!DataHandler.dictCTs.TryGetValue(strOrigName, out condTrigger))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		CondTrigger condTrigger2 = null;
		if (!DataHandler.dictCTs.TryGetValue(text, out condTrigger2))
		{
			condTrigger2 = condTrigger.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public void Destroy()
	{
		this.aReqs = null;
		this.aForbids = null;
	}

	public bool IsBlank()
	{
		if ((this._isBlank || this._valuesWereChanged) && ((double)this.fChance < 1.0 || this.aReqs.Length != 0 || this.aForbids.Length != 0 || this.aTriggers.Length != 0 || this.aTriggersForbid.Length != 0 || this.aLowerConds.Length != 0))
		{
			this._isBlank = false;
		}
		return this._isBlank;
	}

	public bool CloserHighlight(string strCondCheck, string strRoot)
	{
		if (strCondCheck == null)
		{
			return false;
		}
		if (this.IsBlank() && strRoot != null)
		{
			return false;
		}
		bool flag = strRoot == null;
		bool flag2 = false;
		foreach (string text in this.aReqs)
		{
			if (text == strCondCheck)
			{
				flag2 = true;
			}
			if (strRoot != null && text.IndexOf(strRoot) == 0)
			{
				flag = true;
			}
		}
		foreach (string strName in this.aTriggers)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger(strName);
			if (flag)
			{
				flag2 = condTrigger.CloserHighlight(strCondCheck, null);
			}
			else
			{
				flag2 = condTrigger.CloserHighlight(strCondCheck, strRoot);
			}
		}
		if (flag && this.aReqs.Length + this.aTriggers.Length == 0)
		{
			flag2 = true;
		}
		foreach (string text2 in this.aForbids)
		{
			if (strCondCheck == text2)
			{
				return false;
			}
			if (strRoot != null && text2.IndexOf(strRoot) == 0)
			{
				flag = true;
			}
		}
		foreach (string strTrig in this.aTriggersForbid)
		{
			CondTrigger trigger = this.GetTrigger(strTrig, CondTrigger.CTDict.Forbids);
			if (flag)
			{
				flag2 = trigger.CloserHighlight(strCondCheck, null);
			}
			else
			{
				flag2 = trigger.CloserHighlight(strCondCheck, strRoot);
			}
		}
		return flag && flag2;
	}

	public List<string> GetCloserHighlights(List<string> aDCs)
	{
		List<string> list = new List<string>();
		if (this.IsBlank())
		{
			return list;
		}
		bool flag = false;
		foreach (string item in this.aReqs)
		{
			if (aDCs.IndexOf(item) >= 0)
			{
				if (list.IndexOf(item) < 0)
				{
					list.Add(item);
				}
				flag = true;
			}
		}
		if (!flag && this.aForbids.Length > 0)
		{
			foreach (string text in aDCs)
			{
				if (Array.IndexOf<string>(this.aForbids, text) >= 0)
				{
					flag = true;
				}
				else
				{
					list.Add(text);
				}
			}
		}
		if (!flag)
		{
			list.Clear();
		}
		foreach (string strTrig in this.aTriggers)
		{
			CondTrigger trigger = this.GetTrigger(strTrig, CondTrigger.CTDict.Triggers);
			List<string> closerHighlights = trigger.GetCloserHighlights(aDCs);
			foreach (string item2 in closerHighlights)
			{
				if (list.IndexOf(item2) < 0)
				{
					list.Add(item2);
				}
			}
		}
		foreach (string strTrig2 in this.aTriggersForbid)
		{
			CondTrigger trigger2 = this.GetTrigger(strTrig2, CondTrigger.CTDict.Forbids);
			List<string> closerHighlights2 = trigger2.GetCloserHighlights(aDCs);
			foreach (string item3 in closerHighlights2)
			{
				if (list.IndexOf(item3) < 0)
				{
					list.Add(item3);
				}
			}
		}
		return list;
	}

	public bool Triggered(CondOwner objOwner, string strIAStatsName = null, bool logOutcome = true)
	{
		if (this.logReason)
		{
			this.logReason = logOutcome;
		}
		this.strFailReasonLast = string.Empty;
		if (objOwner == null)
		{
			return false;
		}
		if (this.IsBlank())
		{
			return true;
		}
		objOwner.ValidateParent();
		SocialStats socialStats = null;
		if (strIAStatsName != null && DataHandler.dictSocialStats.TryGetValue(strIAStatsName, out socialStats))
		{
			socialStats.nChecked++;
		}
		if (!CondTrigger.bChanceSkip && this.fChance < 1f)
		{
			float num = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
			if (num > this.fChance)
			{
				if (socialStats != null)
				{
					socialStats.nChecked++;
				}
				if (this.logReason)
				{
					this.strFailReasonLast = string.Concat(new object[]
					{
						"Chance: ",
						num,
						" / ",
						this.fChance
					});
				}
				return false;
			}
		}
		if (this.bAND)
		{
			Condition condition;
			if (this.strHigherCond != null)
			{
				condition = null;
				double num2 = 0.0;
				objOwner.mapConds.TryGetValue(this.strHigherCond, out condition);
				if (condition != null)
				{
					num2 = condition.fCount;
				}
				foreach (string key in this.aLowerConds)
				{
					condition = null;
					double num3;
					if (!objOwner.mapConds.TryGetValue(key, out condition))
					{
						num3 = 0.0;
					}
					else
					{
						num3 = condition.fCount;
					}
					if (num3 > num2)
					{
						return false;
					}
				}
			}
			foreach (string text in this.aReqs)
			{
				if (!objOwner.mapConds.TryGetValue(text, out condition))
				{
					this.StatsTrackReqs(strIAStatsName, text, 1f);
					if (this.logReason)
					{
						this.strFailReasonLast = "Lacking: " + text;
					}
					return false;
				}
				if (condition == null || condition.fCount <= 0.0)
				{
					this.StatsTrackReqs(strIAStatsName, text, 1f);
					if (this.logReason)
					{
						this.strFailReasonLast = "Lacking: " + text;
					}
					return false;
				}
			}
			condition = null;
			foreach (string text2 in this.aForbids)
			{
				if (objOwner.mapConds.TryGetValue(text2, out condition))
				{
					if (condition.fCount > 0.0)
					{
						this.StatsTrackForbids(strIAStatsName, text2, 1f);
						if (this.logReason)
						{
							this.strFailReasonLast = "Forbidden: " + text2;
						}
						return false;
					}
				}
			}
			foreach (string strTrig in this.aTriggers)
			{
				CondTrigger trigger = this.GetTrigger(strTrig, CondTrigger.CTDict.Triggers);
				if (!trigger.Triggered(objOwner, strIAStatsName, this.logReason))
				{
					if (this.logReason)
					{
						this.strFailReasonLast = trigger.strFailReasonLast;
					}
					return false;
				}
			}
			return true;
		}
		foreach (string text3 in this.aForbids)
		{
			Condition condition;
			if (objOwner.mapConds.TryGetValue(text3, out condition))
			{
				if (condition.fCount > 0.0)
				{
					this.StatsTrackForbids(strIAStatsName, text3, 1f);
					if (this.logReason)
					{
						this.strFailReasonLast = "Forbidden: " + text3;
					}
					return false;
				}
			}
		}
		foreach (string strTrig2 in this.aTriggersForbid)
		{
			CondTrigger trigger2 = this.GetTrigger(strTrig2, CondTrigger.CTDict.Forbids);
			if (!trigger2.Triggered(objOwner, strIAStatsName, this.logReason))
			{
				if (this.logReason)
				{
					this.strFailReasonLast = trigger2.strFailReasonLast;
				}
				return false;
			}
		}
		if (this.strHigherCond != null)
		{
			Condition condition = null;
			double num4 = 0.0;
			objOwner.mapConds.TryGetValue(this.strHigherCond, out condition);
			if (condition != null)
			{
				num4 = condition.fCount;
			}
			foreach (string key2 in this.aLowerConds)
			{
				condition = null;
				if (objOwner.mapConds.TryGetValue(key2, out condition))
				{
					if (condition.fCount <= num4)
					{
						return true;
					}
				}
			}
		}
		string text4 = "Lacking: (";
		bool flag = false;
		foreach (string text5 in this.aReqs)
		{
			if (this.logReason)
			{
				text4 = text4 + text5 + " ";
			}
			flag = true;
			Condition condition;
			if (objOwner.mapConds.TryGetValue(text5, out condition) && condition != null)
			{
				if (condition.fCount > 0.0)
				{
					return true;
				}
			}
		}
		if (flag && this.logReason)
		{
			this.strFailReasonLast = this.strFailReasonLast + text4 + ")";
		}
		if (this.logReason)
		{
			text4 = "Triggers Lacking: (";
		}
		flag = false;
		foreach (string strTrig3 in this.aTriggers)
		{
			CondTrigger trigger3 = this.GetTrigger(strTrig3, CondTrigger.CTDict.Triggers);
			if (trigger3.Triggered(objOwner, strIAStatsName, this.logReason))
			{
				return true;
			}
			if (this.logReason)
			{
				text4 = text4 + trigger3.strFailReasonLast + " ";
			}
			flag = true;
		}
		if (flag && this.logReason)
		{
			this.strFailReasonLast = this.strFailReasonLast + text4 + ")";
		}
		if (this.aReqs.Length + this.aTriggers.Length == 0)
		{
			return true;
		}
		foreach (string strCond in this.aReqs)
		{
			this.StatsTrackReqs(strIAStatsName, strCond, 1f / (float)this.aReqs.Length);
		}
		return false;
	}

	public bool TriggeredREL(Relationship rel)
	{
		if (rel == null)
		{
			rel = CondTrigger.relStranger;
		}
		if (this.bAND)
		{
			foreach (string item in this.aReqs)
			{
				if (rel.aRelationships.IndexOf(item) < 0)
				{
					return false;
				}
			}
			foreach (string item2 in this.aForbids)
			{
				if (rel.aRelationships.IndexOf(item2) >= 0)
				{
					return false;
				}
			}
			foreach (string strTrig in this.aTriggers)
			{
				CondTrigger trigger = this.GetTrigger(strTrig, CondTrigger.CTDict.Triggers);
				if (!trigger.TriggeredREL(rel))
				{
					return false;
				}
			}
			return true;
		}
		foreach (string item3 in this.aForbids)
		{
			if (rel.aRelationships.IndexOf(item3) >= 0)
			{
				return false;
			}
		}
		foreach (string strTrig2 in this.aTriggersForbid)
		{
			CondTrigger trigger2 = this.GetTrigger(strTrig2, CondTrigger.CTDict.Forbids);
			if (!trigger2.TriggeredREL(rel))
			{
				return false;
			}
		}
		foreach (string item4 in this.aReqs)
		{
			if (rel.aRelationships.IndexOf(item4) >= 0)
			{
				return true;
			}
		}
		foreach (string strTrig3 in this.aTriggers)
		{
			CondTrigger trigger3 = this.GetTrigger(strTrig3, CondTrigger.CTDict.Triggers);
			if (trigger3.TriggeredREL(rel))
			{
				return true;
			}
		}
		return this.aReqs.Length + this.aTriggers.Length == 0;
	}

	public bool TriggeredREL(CondOwner coUs, CondOwner coThem)
	{
		if (coUs == null || coThem == null)
		{
			return false;
		}
		coUs.ValidateParent();
		coThem.ValidateParent();
		Relationship relationship = null;
		if (coUs.socUs != null)
		{
			relationship = coUs.socUs.GetRelationship(coThem.strName);
		}
		if (relationship == null)
		{
			relationship = CondTrigger.relStranger;
		}
		return this.TriggeredREL(relationship);
	}

	private void StatsTrackForbids(string strIAStatsName, string strCond, float fCount)
	{
		if (strIAStatsName != null && DataHandler.dictSocialStats.ContainsKey(strIAStatsName))
		{
			if (!DataHandler.dictSocialStats[strIAStatsName].dictForbids.ContainsKey(strCond))
			{
				DataHandler.dictSocialStats[strIAStatsName].dictForbids[strCond] = 0f;
			}
			Dictionary<string, float> dictForbids;
			(dictForbids = DataHandler.dictSocialStats[strIAStatsName].dictForbids)[strCond] = dictForbids[strCond] + fCount;
			DataHandler.dictSocialStats[strIAStatsName].fForbids += fCount;
		}
	}

	private void StatsTrackReqs(string strIAStatsName, string strCond, float fCount)
	{
		if (strIAStatsName != null && DataHandler.dictSocialStats.ContainsKey(strIAStatsName))
		{
			if (!DataHandler.dictSocialStats[strIAStatsName].dictReqs.ContainsKey(strCond))
			{
				DataHandler.dictSocialStats[strIAStatsName].dictReqs[strCond] = 0f;
			}
			Dictionary<string, float> dictReqs;
			(dictReqs = DataHandler.dictSocialStats[strIAStatsName].dictReqs)[strCond] = dictReqs[strCond] + fCount;
			DataHandler.dictSocialStats[strIAStatsName].fReqs += fCount;
		}
	}

	public void ApplyChanceID(bool bAdd, CondOwner objOwner, float fCoeff = 1f, float fAge = 0f)
	{
		if (objOwner == null)
		{
			return;
		}
		if (!bAdd)
		{
			fCoeff = -1f;
		}
		if (this.fCount != 0f)
		{
			objOwner.AddCondAmount(this.strCondName, (double)(fCoeff * this.fCount), (double)fAge, 0f);
		}
	}

	public List<string> GetAllReqNames(bool bForbids = false)
	{
		List<string> list = new List<string>();
		if (bForbids)
		{
			list.AddRange(this.aForbids);
		}
		else
		{
			list.AddRange(this.aReqs);
		}
		foreach (string strTrig in this.aTriggers)
		{
			CondTrigger trigger = this.GetTrigger(strTrig, CondTrigger.CTDict.Triggers);
			list.AddRange(trigger.GetAllReqNames(bForbids));
		}
		foreach (string strTrig2 in this.aTriggersForbid)
		{
			list.AddRange(this.GetTrigger(strTrig2, CondTrigger.CTDict.Forbids).GetAllReqNames(bForbids));
		}
		return list;
	}

	public string GetReqUsedSuffix(CondOwner coUs)
	{
		if (coUs == null)
		{
			return string.Empty;
		}
		List<string> allReqNames = this.GetAllReqNames(false);
		List<string> list = new List<string>();
		foreach (string text in allReqNames)
		{
			if (coUs.HasCond(text) && GUISocialCombat2.CountsAsSocialReveal(text, true, true, true, true))
			{
				list.Add(coUs.mapConds[text].strNameFriendly);
			}
		}
		if (list.Count > 0)
		{
			string str = "(";
			int index = MathUtils.Rand(0, list.Count, MathUtils.RandType.Flat, null);
			index = 0;
			str += list[index];
			return str + ")";
		}
		return string.Empty;
	}

	public override string ToString()
	{
		return this.strName;
	}

	public IDictionary<string, IEnumerable> GetVerifiables()
	{
		IDictionary<string, IEnumerable> verifiables = new Dictionary<string, IEnumerable>();
		Action<string[]> action = delegate(string[] stringArray)
		{
			if (stringArray != null)
			{
				foreach (string text in stringArray)
				{
					if (!string.IsNullOrEmpty(text))
					{
						verifiables.TryAdd(text, new Type[]
						{
							typeof(CondTrigger),
							typeof(JsonCond)
						});
					}
				}
			}
		};
		action(this.aReqs);
		action(this.aForbids);
		action(this.aTriggers);
		action(this.aTriggersForbid);
		return verifiables;
	}

	public string RulesInfo
	{
		get
		{
			if (this.strFailReason != null)
			{
				return this.strFailReason;
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (this.bAND)
			{
				for (int i = 0; i < this.aReqs.Length; i++)
				{
					if (i == 0)
					{
						if (stringBuilder.Length > 0)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is ");
					}
					else if (i == this.aReqs.Length - 1)
					{
						stringBuilder.Append(", and ");
					}
					else if (i > 0)
					{
						stringBuilder.Append(", ");
					}
					Condition cond = DataHandler.GetCond(this.aReqs[i]);
					stringBuilder.Append(cond.strNameFriendly);
					if (i == this.aReqs.Length - 1)
					{
						stringBuilder.Append(".");
					}
				}
				for (int j = 0; j < this.aForbids.Length; j++)
				{
					if (j == 0)
					{
						if (stringBuilder.Length > 0)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is NOT ");
					}
					else if (j == this.aForbids.Length - 1)
					{
						stringBuilder.Append(", and ");
					}
					else if (j > 0)
					{
						stringBuilder.Append(", ");
					}
					Condition cond = DataHandler.GetCond(this.aForbids[j]);
					stringBuilder.Append(cond.strNameFriendly);
					if (j == this.aForbids.Length - 1)
					{
						stringBuilder.Append(".");
					}
				}
				foreach (string strTrig in this.aTriggers)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(" ");
					}
					CondTrigger trigger = this.GetTrigger(strTrig, CondTrigger.CTDict.Triggers);
					stringBuilder.Append(trigger.RulesInfo);
				}
			}
			else
			{
				for (int l = 0; l < this.aForbids.Length; l++)
				{
					if (l == 0)
					{
						if (stringBuilder.Length > 0)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is NOT ");
					}
					else if (l == this.aForbids.Length - 1)
					{
						stringBuilder.Append(", or ");
					}
					else if (l > 0)
					{
						stringBuilder.Append(", ");
					}
					Condition cond = DataHandler.GetCond(this.aForbids[l]);
					stringBuilder.Append(cond.strNameFriendly);
					if (l == this.aForbids.Length - 1)
					{
						stringBuilder.Append(".");
					}
				}
				for (int m = 0; m < this.aReqs.Length; m++)
				{
					if (m == 0)
					{
						if (stringBuilder.Length > 0)
						{
							stringBuilder.Append(" ");
						}
						stringBuilder.Append("Is ");
					}
					else if (m == this.aReqs.Length - 1)
					{
						stringBuilder.Append(", or ");
					}
					else if (m > 0)
					{
						stringBuilder.Append(", ");
					}
					Condition cond = DataHandler.GetCond(this.aReqs[m]);
					stringBuilder.Append(cond.strNameFriendly);
					if (m == this.aReqs.Length - 1)
					{
						stringBuilder.Append(".");
					}
				}
				foreach (string strTrig2 in this.aTriggers)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(" ");
					}
					CondTrigger trigger2 = this.GetTrigger(strTrig2, CondTrigger.CTDict.Triggers);
					stringBuilder.Append(trigger2.RulesInfo);
				}
				foreach (string strTrig3 in this.aTriggersForbid)
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Append(" ");
					}
					CondTrigger trigger3 = this.GetTrigger(strTrig3, CondTrigger.CTDict.Forbids);
					stringBuilder.Append(trigger3.RulesInfo);
				}
			}
			this.strFailReason = stringBuilder.ToString();
			return this.strFailReason;
		}
	}

	private CondTrigger GetTrigger(string strTrig, CondTrigger.CTDict dict)
	{
		Dictionary<string, CondTrigger> dictionary = (dict != CondTrigger.CTDict.Triggers) ? this.aTriggersForbidConds : this.aTriggersConds;
		if (dictionary == null)
		{
			return DataHandler.GetCondTrigger(strTrig);
		}
		CondTrigger condTrigger;
		if (!dictionary.TryGetValue(strTrig, out condTrigger) || condTrigger == null)
		{
			condTrigger = DataHandler.GetCondTrigger(strTrig);
			dictionary[strTrig] = condTrigger;
		}
		return condTrigger;
	}

	private CondTrigger SetTrigger(string strTrig, CondTrigger.CTDict dict)
	{
		Dictionary<string, CondTrigger> dictionary = (dict != CondTrigger.CTDict.Triggers) ? this.aTriggersForbidConds : this.aTriggersConds;
		if (dictionary == null)
		{
			dictionary = new Dictionary<string, CondTrigger>();
			if (dict == CondTrigger.CTDict.Forbids)
			{
				this.aTriggersForbidConds = dictionary;
			}
			else
			{
				this.aTriggersConds = dictionary;
			}
		}
		CondTrigger condTrigger;
		if (!dictionary.TryGetValue(strTrig, out condTrigger) || condTrigger == null)
		{
			condTrigger = DataHandler.GetCondTrigger(strTrig);
			dictionary[strTrig] = condTrigger;
		}
		return condTrigger;
	}

	private float _fChance;

	private float _fCount;

	public bool bAND = true;

	private int nRecursion;

	private static readonly string[] _aDefault = new string[0];

	private string[] _aReqs;

	private string[] _aForbids;

	private string[] _aTriggers;

	private string[] _aTriggersForbid;

	private string[] _aLowerConds;

	private Dictionary<string, CondTrigger> aTriggersConds;

	private Dictionary<string, CondTrigger> aTriggersForbidConds;

	private string strFailReason;

	public string strFailReasonLast;

	private static Relationship relStranger;

	public static bool bChanceSkip = false;

	public bool logReason = true;

	private bool _valuesWereChanged;

	private bool? _requiresHumans;

	public int nFilterMultiple;

	private bool _isBlank = true;

	private enum CTDict
	{
		Triggers,
		Forbids
	}
}
