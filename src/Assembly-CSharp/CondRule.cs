using System;

// Threshold rule that reacts when a tracked condition crosses bands.
// This appears to be the bridge between raw condition amounts and secondary
// loot/effect changes such as status transitions, UI priority, or side effects.
public class CondRule
{
	// Default ctor for deserialization and empty threshold setup.
	public CondRule()
	{
		this.fPref = double.PositiveInfinity;
		this.aThresholds = new CondRuleThresh[0];
	}

	public string strName { get; set; }

	public string strCond { get; set; }

	public double fPref { get; set; }

	public CondRuleThresh[] aThresholds { get; set; }

	// Shallow copy for rule reuse.
	public CondRule Clone()
	{
		return new CondRule
		{
			strName = this.strName,
			strCond = this.strCond,
			fPref = this.fPref,
			aThresholds = this.aThresholds,
			fModifier = this.fModifier
		};
	}

	// Deep copy with linked ids remapped for generated condition families.
	public CondRule CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		CondRule condRule = this.Clone();
		condRule.strName = this.strName.Replace(strFind, strReplace);
		condRule.strCond = JsonCond.CloneDeep(this.strCond, strReplace, strFind);
		if (this.aThresholds != null)
		{
			condRule.aThresholds = new CondRuleThresh[this.aThresholds.Length];
			for (int i = 0; i < this.aThresholds.Length; i++)
			{
				condRule.aThresholds[i] = this.aThresholds[i].Clone();
				condRule.aThresholds[i].strLootNew = Loot.CloneDeep(this.aThresholds[i].strLootNew, strReplace, strFind);
			}
		}
		return condRule;
	}

	// Helper for remapping a saved/rule id through the shared registry.
	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		CondRule condRule = null;
		if (!DataHandler.dictCondRules.TryGetValue(strOrigName, out condRule))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		CondRule value = null;
		if (!DataHandler.dictCondRules.TryGetValue(text, out value))
		{
			value = condRule.CloneDeep(strFind, strReplace);
			DataHandler.dictCondRules[text] = value;
		}
		return text;
	}

	// Changes the effective threshold scale and reapplies the tracked condition if needed.
	public void ChangeThresh(CondOwner co, double fAmount)
	{
		if (co == null || fAmount == 0.0)
		{
			return;
		}
		double num = this.fModifier;
		this.fModifier += fAmount;
		double num2 = this.fModifier;
		if (num <= 0.0)
		{
			num = 1E-08;
		}
		if (num2 <= 0.0)
		{
			num2 = 1E-08;
		}
		Condition condition = null;
		co.mapConds.TryGetValue(this.strCond, out condition);
		if (condition != null && num != num2)
		{
			double num3 = num2 / num;
			this.ChangeStat(co, condition.fCount * num3, condition.fCount);
			co.UpdatePriority(condition);
		}
	}

	// Applies threshold crossings between old and new values, usually by firing loot scripts.
	public void ChangeStat(CondOwner co, double fOld, double fNew)
	{
		if (co == null || fOld == fNew)
		{
			return;
		}
		double num = this.fModifier;
		if (num <= 0.0)
		{
			num = 1E-08;
		}
		this.nNesting++;
		this.nNestingMax = this.nNesting;
		for (int i = 0; i < this.aThresholds.Length; i++)
		{
			float num2 = 0f;
			CondRuleThresh condRuleThresh = this.aThresholds[i];
			if (fNew < fOld)
			{
				condRuleThresh = this.aThresholds[this.aThresholds.Length - 1 - i];
			}
			if (fNew > fOld)
			{
				if (MathUtils.CompareLT(fOld, (double)condRuleThresh.fMin * num, 1E-05f) && MathUtils.CompareGTE(fNew, (double)condRuleThresh.fMin * num, 1E-05f))
				{
					num2 += condRuleThresh.fMinAdd;
				}
				if (MathUtils.CompareLT(fOld, (double)condRuleThresh.fMax * num, 1E-05f) && MathUtils.CompareGTE(fNew, (double)condRuleThresh.fMax * num, 1E-05f))
				{
					num2 += condRuleThresh.fMaxAdd;
				}
			}
			else
			{
				if (MathUtils.CompareGTE(fOld, (double)condRuleThresh.fMin * num, 1E-05f) && MathUtils.CompareLT(fNew, (double)condRuleThresh.fMin * num, 1E-05f))
				{
					num2 -= condRuleThresh.fMinAdd;
				}
				if (MathUtils.CompareGTE(fOld, (double)condRuleThresh.fMax * num, 1E-05f) && MathUtils.CompareLT(fNew, (double)condRuleThresh.fMax * num, 1E-05f))
				{
					num2 -= condRuleThresh.fMaxAdd;
				}
			}
			if (num2 != 0f)
			{
				Loot loot = DataHandler.GetLoot(condRuleThresh.strLootNew);
				if (loot.strName != "Blank")
				{
					bool bLogConds = co.bLogConds;
					if (num2 < 0f)
					{
						co.bLogConds = false;
					}
					loot.ApplyCondLoot(co, num2, null, (float)(fNew - fOld));
					co.bLogConds = bLogConds;
				}
			}
			if (this.nNestingMax > this.nNesting)
			{
				break;
			}
		}
		this.nNesting--;
		if (this.nNesting == 0)
		{
			this.nNestingMax = 0;
		}
	}

	public CondRuleThresh GetCurrentThresh(CondOwner co, double fValue)
	{
		if (co == null)
		{
			return null;
		}
		double num = this.fModifier;
		if (num <= 0.0)
		{
			num = 1E-08;
		}
		for (int i = 0; i < this.aThresholds.Length; i++)
		{
			CondRuleThresh condRuleThresh = this.aThresholds[i];
			if (fValue >= (double)condRuleThresh.fMin * num && fValue < (double)condRuleThresh.fMax * num)
			{
				return condRuleThresh;
			}
		}
		return null;
	}

	public CondRuleThresh GetCurrentThresh(CondOwner co)
	{
		if (co == null)
		{
			return null;
		}
		return this.GetCurrentThresh(co, co.GetCondAmount(this.strCond));
	}

	// Save payload is compact: `ruleName=modifier`.
	public string GetSaveInfo()
	{
		return this.strName + "=" + this.fModifier;
	}

	// Rebuilds a rule reference plus modifier from the compact save string.
	public static CondRule LoadSaveInfo(string strDef)
	{
		if (strDef == null)
		{
			return null;
		}
		string[] array = strDef.Split(new char[]
		{
			'='
		});
		if (array.Length == 0)
		{
			return null;
		}
		CondRule condRule = DataHandler.GetCondRule(array[0]);
		if (condRule == null)
		{
			return null;
		}
		if (array.Length > 1)
		{
			double.TryParse(array[1], out condRule.fModifier);
		}
		return condRule;
	}

	public override string ToString()
	{
		return this.strName;
	}

	public double Modifier
	{
		get
		{
			return this.fModifier;
		}
	}

	public double Preference
	{
		get
		{
			return this.fModifier * this.fPref;
		}
	}

	private double fModifier = 1.0;

	private int nNesting;

	private int nNestingMax;
}
