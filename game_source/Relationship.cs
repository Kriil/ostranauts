using System;
using System.Collections.Generic;
using UnityEngine;

public class Relationship
{
	public Relationship()
	{
		this.aReveals = new List<string>();
		this.aRelationships = new List<string>();
		this.aEvents = new List<string>();
	}

	public Relationship(PersonSpec pspec, List<string> aRels, List<string> aEvs)
	{
		this.pspec = pspec;
		this.aRelationships = aRels;
		if (this.aRelationships == null)
		{
			this.aRelationships = new List<string>();
		}
		this.aEvents = aEvs;
		if (this.aEvents == null)
		{
			this.aEvents = new List<string>();
		}
		this.aReveals = new List<string>();
		Dictionary<string, double> dictionary = new Dictionary<string, double>();
		foreach (string str in this.aRelationships)
		{
			Loot loot = DataHandler.GetLoot("COND" + str);
			List<string> lootNames = loot.GetLootNames(null, false, null);
			foreach (string text in lootNames)
			{
				if (text.IndexOf('-') == 0)
				{
					if (dictionary.ContainsKey(text.Substring(1)))
					{
						Dictionary<string, double> dictionary2;
						string key;
						(dictionary2 = dictionary)[key = text.Substring(1)] = dictionary2[key] + -1.0;
					}
					else
					{
						dictionary[text.Substring(1)] = -1.0;
					}
				}
				else if (dictionary.ContainsKey(text))
				{
					Dictionary<string, double> dictionary2;
					string key2;
					(dictionary2 = dictionary)[key2 = text] = dictionary2[key2] + 1.0;
				}
				else
				{
					dictionary[text] = 1.0;
				}
			}
		}
		this.StoreCond(dictionary);
	}

	public Relationship(JsonRelationship jr, PersonSpec pspec)
	{
		if (jr == null || pspec == null)
		{
			Debug.Log("ERROR: Initializing a relationship with null.");
			return;
		}
		this.pspec = pspec;
		if (jr.aRelationships == null)
		{
			this.aRelationships = new List<string>();
		}
		else
		{
			this.aRelationships = new List<string>(jr.aRelationships);
		}
		if (jr.aEvents == null)
		{
			this.aEvents = new List<string>();
		}
		else
		{
			this.aEvents = new List<string>(jr.aEvents);
		}
		if (jr.aReveals == null)
		{
			this.aReveals = new List<string>();
		}
		else
		{
			this.aReveals = new List<string>(jr.aReveals);
		}
		this.fAnimosity = jr.fAnimosity;
		this.fFamiliarity = jr.fFamiliarity;
		this.fKindness = jr.fKindness;
		this.strContext = jr.strContext;
		this.dictConds = this.Conds;
		if (jr.dictConds != null)
		{
			foreach (KeyValuePair<string, double> keyValuePair in jr.dictConds)
			{
				this.Conds[keyValuePair.Key] = keyValuePair.Value;
			}
		}
	}

	public static string GetReciprocalREL(string strREL, CondOwner coReciprocal = null)
	{
		if (string.IsNullOrEmpty(strREL))
		{
			return strREL;
		}
		if (strREL != null)
		{
			if (strREL == "RELBioFather")
			{
				return "RELBioChild";
			}
			if (strREL == "RELBioMother")
			{
				return "RELBioChild";
			}
			if (!(strREL == "RELBioChild"))
			{
				if (strREL == "RELCaptain")
				{
					return "RELCrew";
				}
			}
			else
			{
				if (coReciprocal != null)
				{
					if (coReciprocal.HasCond("IsFemale"))
					{
						return "RELBioMother";
					}
					if (coReciprocal.HasCond("IsMale"))
					{
						return "RELBioFather";
					}
				}
				if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.55)
				{
					return "RELBioMother";
				}
				return "RELBioFather";
			}
		}
		return strREL;
	}

	public void AddRelationship(CondOwner coUs, string strREL)
	{
		if (coUs == null)
		{
			return;
		}
		Condition cond = DataHandler.GetCond(strREL);
		if (cond == null || this.aRelationships.IndexOf(cond.strName) >= 0)
		{
			return;
		}
		this.aRelationships.Add(cond.strName);
		string strNameFriendly = cond.strNameFriendly;
		string strMsg = string.Concat(new string[]
		{
			coUs.FriendlyName,
			" now considers ",
			this.pspec.FullName,
			" a(n) ",
			strNameFriendly,
			"!"
		});
		coUs.LogMessage(strMsg, cond.strColor, coUs.strName);
		if (this.pspec.GetCO() != null)
		{
			this.pspec.GetCO().LogMessage(strMsg, cond.strColor, this.pspec.GetCO().strName);
		}
	}

	public void RemoveRelationship(CondOwner coUs, string strREL, bool bVerbose = false)
	{
		if (coUs == null)
		{
			return;
		}
		Condition cond = DataHandler.GetCond(strREL);
		if (cond == null || this.aRelationships.IndexOf(cond.strName) < 0)
		{
			return;
		}
		this.aRelationships.Remove(cond.strName);
		if (!bVerbose)
		{
			return;
		}
		string strNameFriendly = cond.strNameFriendly;
		string strMsg = string.Concat(new string[]
		{
			coUs.FriendlyName,
			" no longer considers ",
			this.pspec.FullName,
			" a(n) ",
			strNameFriendly,
			"!"
		});
		coUs.LogMessage(strMsg, cond.strColor, coUs.strName);
		if (this.pspec.GetCO() != null)
		{
			this.pspec.GetCO().LogMessage(strMsg, cond.strColor, this.pspec.GetCO().strName);
		}
	}

	public void StoreCond(CondOwner coUs, Dictionary<string, double> dict, CondOwner coThem = null)
	{
		if (dict == null || coUs == null || !coUs.bAlive || coUs.HasCond("Unconscious"))
		{
			return;
		}
		this.StoreCond(dict);
		if (this.Conds["StatIntimacy"] <= -50.0 && this.aRelationships.IndexOf("RELLover") < 0)
		{
			bool flag = coUs.HasCond("IsAttractedMen") && coThem.HasCond("IsMale");
			if (!flag && coUs.HasCond("IsAttractedWomen") && coThem.HasCond("IsFemale"))
			{
				flag = true;
			}
			else if (!flag && coUs.HasCond("IsAttractedNB") && coThem.HasCond("IsNB"))
			{
				flag = true;
			}
			if (flag)
			{
				this.RemoveRelationship(coUs, "RELAcquaintance", false);
				this.RemoveRelationship(coUs, "RELStranger", false);
				this.RemoveRelationship(coUs, "RELLoverEx", false);
				this.AddRelationship(coUs, "RELLover");
			}
		}
		if (this.Conds["StatIntimacy"] > -20.0 && this.aRelationships.IndexOf("RELLover") >= 0)
		{
			this.RemoveRelationship(coUs, "RELLover", false);
			this.AddRelationship(coUs, "RELLoverEx");
		}
		if (this.fFamiliarity >= 200.0 && this.aRelationships.IndexOf("RELStranger") < 0)
		{
			if (this.fAnimosity / this.fFamiliarity > 0.55)
			{
				if (this.aRelationships.IndexOf("RELEnemy") < 0 && this.aRelationships.IndexOf("RELNemesis") < 0)
				{
					this.RemoveRelationship(coUs, "RELAcquaintance", false);
					this.RemoveRelationship(coUs, "RELFriend", false);
					this.AddRelationship(coUs, "RELEnemy");
				}
			}
			else if (this.fKindness / this.fFamiliarity > 0.55 && this.aRelationships.IndexOf("RELFriend") < 0)
			{
				this.RemoveRelationship(coUs, "RELAcquaintance", false);
				this.RemoveRelationship(coUs, "RELEnemy", false);
				this.AddRelationship(coUs, "RELFriend");
			}
		}
		else if (this.fFamiliarity >= 50.0 && this.aRelationships.IndexOf("RELStranger") >= 0)
		{
			this.RemoveRelationship(coUs, "RELStranger", false);
			this.AddRelationship(coUs, "RELAcquaintance");
		}
	}

	public void StoreCond(Dictionary<string, double> dict)
	{
		if (dict == null)
		{
			return;
		}
		this.fFamiliarity = 0.0;
		this.fAnimosity = 0.0;
		this.fKindness = 0.0;
		foreach (string text in this.CondsThatMatter)
		{
			if (!this.Conds.ContainsKey(text))
			{
				Debug.Log("ERROR: dictConds has missing entries. Re-adding now.");
				this.Conds[text] = 0.0;
			}
			if (dict.ContainsKey(text))
			{
				Dictionary<string, double> conds;
				string key;
				(conds = this.Conds)[key = text] = conds[key] + Relationship.fFraction * dict[text];
			}
			double num = this.Conds[text];
			this.fFamiliarity += Math.Abs(num);
			if (num < 0.0)
			{
				this.fKindness -= num;
			}
			else
			{
				this.fAnimosity += num;
			}
		}
	}

	public void ApplyConds(CondOwner coUs, bool bRemove = false)
	{
		if (coUs == null)
		{
			return;
		}
		int num = 1;
		if (bRemove)
		{
			num = -1;
		}
		foreach (KeyValuePair<string, double> keyValuePair in this.Conds)
		{
			double num2 = keyValuePair.Value;
			if (num2 > Relationship.fMax)
			{
				num2 = Relationship.fMax;
			}
			else if (num2 < -Relationship.fMax)
			{
				num2 = -Relationship.fMax;
			}
			coUs.AddCondAmount(keyValuePair.Key, (double)num * num2, 0.0, 0f);
		}
	}

	public void RevealDefaults()
	{
		if (Relationship.aTraitsFilter == null)
		{
			Relationship.aTraitsFilter = DataHandler.GetLoot("CONDSocialGUIFilter").GetLootNames(null, false, null);
		}
		double num = 0.0;
		if (this.aRelationships.IndexOf("RELLover") >= 0)
		{
			num = 0.75;
		}
		else if (this.aRelationships.IndexOf("RELLoverEx") >= 0)
		{
			num = 0.75;
		}
		else if (this.aRelationships.IndexOf("RELNemesis") >= 0)
		{
			num = 0.75;
		}
		else if (this.aRelationships.IndexOf("RELEnemy") >= 0)
		{
			num = 0.5;
		}
		else if (this.aRelationships.IndexOf("RELFriend") >= 0)
		{
			num = 0.5;
		}
		else if (this.aRelationships.IndexOf("RELAcquaintance") >= 0)
		{
			num = 0.25;
		}
		else if (this.aRelationships.IndexOf("RELContact") >= 0)
		{
			num = 0.25;
		}
		if (num == 0.0)
		{
			return;
		}
		foreach (string text in Relationship.aTraitsFilter)
		{
			float value = UnityEngine.Random.value;
			if ((double)value <= num && this.aReveals.IndexOf(text) < 0)
			{
				Condition cond = DataHandler.GetCond(text);
				if (cond != null && cond.nDisplayOther != 0 && cond.nDisplayOther != 3)
				{
					this.aReveals.Add(text);
				}
			}
		}
	}

	public JsonRelationship GetJson()
	{
		JsonRelationship jsonRelationship = new JsonRelationship();
		jsonRelationship.strPSpec = this.pspec.FullName;
		jsonRelationship.aRelationships = this.aRelationships.ToArray();
		jsonRelationship.aEvents = this.aEvents.ToArray();
		jsonRelationship.aReveals = this.aReveals.ToArray();
		jsonRelationship.fAnimosity = this.fAnimosity;
		jsonRelationship.fFamiliarity = this.fFamiliarity;
		jsonRelationship.fKindness = this.fKindness;
		jsonRelationship.strContext = this.strContext;
		jsonRelationship.dictConds = new Dictionary<string, double>();
		foreach (KeyValuePair<string, double> keyValuePair in this.Conds)
		{
			jsonRelationship.dictConds.Add(keyValuePair.Key, keyValuePair.Value);
		}
		return jsonRelationship;
	}

	public Dictionary<string, double> Conds
	{
		get
		{
			if (this.dictConds == null)
			{
				this.dictConds = new Dictionary<string, double>();
				foreach (string key in this.CondsThatMatter)
				{
					this.dictConds[key] = 0.0;
				}
			}
			if (this.dictConds.Count == 0)
			{
				Debug.Log("ERROR: dictConds initialized with 0 entries. Should be non-zero.");
			}
			return this.dictConds;
		}
		set
		{
			if (value == null)
			{
				Debug.Log("ERROR: dictConds cannot be set to null.");
			}
			this.dictConds = value;
		}
	}

	public bool IsContextDefault
	{
		get
		{
			return string.IsNullOrEmpty(this.strContext) || this.strContext == "Default";
		}
	}

	private List<string> CondsThatMatter
	{
		get
		{
			if (Relationship.aCondsThatMatter == null)
			{
				Relationship.aCondsThatMatter = new List<string>();
				Loot loot = DataHandler.GetLoot("CONDSocialRELStats");
				List<string> lootNames = loot.GetLootNames(null, false, null);
				foreach (string item in lootNames)
				{
					Relationship.aCondsThatMatter.Add(item);
				}
			}
			return Relationship.aCondsThatMatter;
		}
	}

	public override string ToString()
	{
		return this.pspec.FullName;
	}

	public PersonSpec pspec;

	public List<string> aRelationships;

	public List<string> aEvents;

	public List<string> aReveals;

	private Dictionary<string, double> dictConds;

	public double fFamiliarity;

	private double fAnimosity;

	private double fKindness;

	public string strContext;

	private static double fFraction = 0.5;

	private static double fMax = 50.0;

	private static List<string> aCondsThatMatter;

	private static List<string> aTraitsFilter;
}
