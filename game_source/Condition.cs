using System;
using System.Collections.Generic;

// Runtime instance of a Condition applied to a CondOwner.
// This is the live status/state object built from JsonCond definitions loaded
// from StreamingAssets/data/conditions.
public class Condition
{
	// Copies the static condition definition into runtime fields and resolves any follow-up CondTriggers.
	public Condition(JsonCond jsonIn)
	{
		if (jsonIn == null)
		{
			return;
		}
		this.strName = jsonIn.strName;
		this.strNameFriendly = jsonIn.strNameFriendly;
		this.strDesc = jsonIn.strDesc;
		this.strColor = jsonIn.strColor;
		this.strAnti = jsonIn.strAnti;
		if (jsonIn.fDuration == 0f)
		{
			this.fDuration = float.PositiveInfinity;
		}
		else
		{
			this.fDuration = jsonIn.fDuration;
		}
		if (jsonIn.fClampMax == 0f)
		{
			this.fClampMax = float.PositiveInfinity;
		}
		else
		{
			this.fClampMax = jsonIn.fClampMax;
		}
		this.bInvert = jsonIn.bInvert;
		this.bFatal = jsonIn.bFatal;
		this.bKO = jsonIn.bKO;
		this.bAlert = jsonIn.bAlert;
		this.nDisplaySelf = jsonIn.nDisplaySelf;
		this.nDisplayOther = jsonIn.nDisplayOther;
		this.nDisplayType = jsonIn.nDisplayType;
		this.strDisplayBonus = jsonIn.strDisplayBonus;
		this.bResetTimer = jsonIn.bResetTimer;
		this.bQABRefresh = jsonIn.bQABRefresh;
		this.bCondRuleTrackAlways = jsonIn.bCondRuleTrackAlways;
		this.bCondRuleTrackInvert = jsonIn.bCondRuleTrackInvert;
		this.strTrackCond = jsonIn.strTrackCond;
		this.bRemoveAll = jsonIn.bRemoveAll;
		this.bPersists = jsonIn.bPersists;
		this.bNegative = jsonIn.bNegative;
		this.bRoom = jsonIn.bRoom;
		this.bRemoveOnLoad = jsonIn.bRemoveOnLoad;
		this.replacementValues = jsonIn.replacementValues;
		this.fConversionFactor = jsonIn.fConversionFactor;
		if (jsonIn.strCTImmune != null)
		{
			this.ctImmune = DataHandler.GetCondTrigger(jsonIn.strCTImmune);
		}
		if (this.ctImmune != null && this.ctImmune.IsBlank())
		{
			this.ctImmune = null;
		}
		this.aNext = new List<CondTrigger>();
		this.aPer = new List<string>();
		if (jsonIn.pairFaceSprite != null)
		{
			this.pairFaceSprite = jsonIn.pairFaceSprite;
		}
		string[] array = jsonIn.aNext;
		if (array != null)
		{
			foreach (string text in array)
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger(text);
				if (condTrigger != null)
				{
					this.aNext.Add(condTrigger);
				}
			}
		}
		array = jsonIn.aPer;
		if (array != null)
		{
			foreach (string item in array)
			{
				this.aPer.Add(item);
			}
		}
	}

	// Advances timers and fires chained triggers when the condition expires.
	// Likely called from CondOwner's per-update condition processing.
	public void Update(float elapsed, CondOwner objOwner)
	{
		this.fAge += elapsed / 60f / 60f;
		if (this.fAge >= this.fDuration && objOwner != null)
		{
			float num = this.fAge - this.fDuration;
			objOwner.ZeroCondAmount(this.strName);
			foreach (CondTrigger condTrigger in this.aNext)
			{
				if (condTrigger.Triggered(objOwner, null, true))
				{
					CondTrigger condTrigger2 = condTrigger;
					bool bAdd = true;
					float num2 = num;
					condTrigger2.ApplyChanceID(bAdd, objOwner, 1f, num2);
				}
			}
		}
	}

	// Releases transient lists when the condition instance is removed.
	public void Destroy()
	{
		this.aNext.Clear();
		this.aNext = null;
		this.aPer.Clear();
		this.aPer = null;
		this.pairFaceSprite = null;
	}

	// Changes the active amount on the owner, enforcing clamps and optional per-step loot effects.
	// `aPer` appears to reference data/loot entries that fire when the integer part changes.
	public void AddAmount(CondOwner objOwner, double fAmount)
	{
		if (fAmount == 0.0)
		{
			return;
		}
		double d = this.fCount;
		if (fAmount < 0.0 && this.bRemoveAll)
		{
			fAmount = -this.fCount;
		}
		if (this.fCount + fAmount < 0.0)
		{
			fAmount = -this.fCount;
		}
		if (this.fCount + fAmount > (double)this.fClampMax)
		{
			this.fCount = (double)this.fClampMax - fAmount;
		}
		this.fCount += fAmount;
		if (this.bResetTimer)
		{
			this.fAge = 0f;
		}
		objOwner.UpdateCondRecords(this);
		if (!objOwner.bFreezeCondRules && this.aPer.Count > 0)
		{
			int num = (int)(Math.Floor(this.fCount) - Math.Floor(d));
			if (num != 0)
			{
				for (int i = 0; i < this.aPer.Count; i++)
				{
					Loot loot = DataHandler.GetLoot(this.aPer[i]);
					loot.ApplyCondLoot(objOwner, (float)num, null, 0f);
				}
			}
		}
	}

	public float GetAge()
	{
		return this.fAge;
	}

	public void SetAge(float fAgeNew)
	{
		this.fAge = fAgeNew;
	}

	public float GetRemain()
	{
		return this.fDuration - this.fAge;
	}

	public override string ToString()
	{
		return this.strName + ": " + this.fCount;
	}

	public string strName;

	public string strNameFriendly;

	public string strDesc;

	public string strAnti;

	public double fCount;

	public string strColor = "neutral";

	public float fDuration = float.PositiveInfinity;

	private float fAge;

	public double fCondRuleTrack;

	public double fCondRuleTrackTime;

	public float fClampMax = float.PositiveInfinity;

	public bool bInvert = true;

	public bool bFatal;

	public bool bKO;

	public bool bAlert;

	public int nDisplaySelf;

	public int nDisplayOther;

	public bool bResetTimer;

	public bool bRemoveAll;

	public bool bPersists;

	public bool bNegative;

	public bool bRoom;

	public bool bRemoveOnLoad;

	public bool bQABRefresh;

	public bool bCondRuleTrackAlways;

	public bool bCondRuleTrackInvert;

	public string strTrackCond;

	public int nDisplayType;

	public float fConversionFactor = 1f;

	public string strDisplayBonus;

	public CondTrigger ctImmune;

	public List<CondTrigger> aNext;

	public List<string> aPer;

	public JsonStringIntPair pairFaceSprite;

	public const int DISPLAY_NEVER = 0;

	public const int DISPLAY_WHEN_REVEALED = 1;

	public const int DISPLAY_ALWAYS = 2;

	public const int DISPLAY_TOOLTIPS = 3;

	public List<InflectedTokenData> replacementValues;
}
