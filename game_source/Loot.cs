using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.JsonTypes.Interfaces;
using Ostranauts.Tools;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

// Data-driven loot table / weighted selection helper. Used across conditions,
// interactions, gas pricing, social filters, markets, and many other systems.
public class Loot : IVerifiable
{
	// Initializes a safe blank loot definition so partially loaded data still has
	// predictable defaults.
	public Loot()
	{
		this._strName = "Blank";
		this.strType = "trigger";
		this._aCOs = Loot._aDefault;
		this._aLoots = Loot._aDefault;
		this.aCOLootUnits = Loot._aLUsDefault;
		this.aOtherLootUnits = Loot._aLUsDefault;
	}

	// Parses the compact `name=chancexmin-max|...` loot syntax into runtime
	// LootUnit groups.
	private List<List<LootUnit>> ParseLootDef(string[] aIn)
	{
		List<List<LootUnit>> list = new List<List<LootUnit>>();
		foreach (string text in aIn)
		{
			string[] array = text.Split(new char[]
			{
				'|'
			});
			List<LootUnit> list2 = new List<LootUnit>();
			foreach (string text2 in array)
			{
				LootUnit lootUnit = new LootUnit();
				lootUnit.bPositive = true;
				string text3 = text2;
				if (text[0] == '-')
				{
					lootUnit.bPositive = !lootUnit.bPositive;
					text3 = text2.Substring(1);
				}
				string[] array3 = text3.Split(new char[]
				{
					'='
				});
				lootUnit.strName = array3[0];
				if (array3.Length < 2)
				{
					JsonLogger.ReportProblem(text + " (loot definition shorter than expected)", ReportTypes.FailingString);
					Debug.Log("Missing Loot Chance Data: " + this.strName);
				}
				else
				{
					array3 = array3[1].Split(new char[]
					{
						'x'
					});
					float.TryParse(array3[0], out lootUnit.fChance);
					if (lootUnit.fChance != 0f)
					{
						if (array3.Length < 2)
						{
							JsonLogger.ReportProblem(text + " (loot definition shorter than expected)", ReportTypes.FailingString);
						}
						array3 = array3[1].Split(new char[]
						{
							'-'
						});
						float num = 0f;
						if (float.TryParse(array3[0], out num))
						{
							lootUnit.fMin = num;
						}
						if (array3.Length > 1)
						{
							num = 0f;
							if (float.TryParse(array3[1], out num))
							{
								lootUnit.fMax = num;
							}
						}
						if (lootUnit.fMax < lootUnit.fMin)
						{
							lootUnit.fMax = lootUnit.fMin;
						}
						list2.Add(lootUnit);
					}
				}
			}
			list.Add(list2);
		}
		return list;
	}

	// Expands stackable condtrig loot into flat 1-count clones for callers that
	// want discrete trigger entries.
	public List<CondTrigger> GetCTLootFlat(CondTrigger objUs, string strRandID = null)
	{
		List<CondTrigger> ctloot = this.GetCTLoot(objUs, strRandID);
		for (int i = 0; i < ctloot.Count; i++)
		{
			CondTrigger condTrigger = ctloot[i];
			condTrigger.fCount = Mathf.Floor(condTrigger.fCount);
			while ((double)condTrigger.fCount > 1.5)
			{
				CondTrigger condTrigger2 = condTrigger.Clone();
				condTrigger2.fCount = 1f;
				ctloot.Add(condTrigger2);
				condTrigger.fCount -= 1f;
			}
		}
		return ctloot;
	}

	// Resolves one condtrig loot roll using the parsed loot-unit groups.
	public List<CondTrigger> GetCTLoot(CondTrigger objUs, string strRandID = null)
	{
		List<CondTrigger> list = new List<CondTrigger>();
		int num = 0;
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			float num2 = 0f;
			string text = strRandID;
			if (text != null)
			{
				text += num;
			}
			num++;
			float fRand = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text);
			foreach (LootUnit lootUnit in list2)
			{
				float num3 = lootUnit.GetAmount(num2, fRand);
				if (!lootUnit.bPositive)
				{
					num3 = -num3;
				}
				num2 += lootUnit.fChance;
				if (num3 > 0f)
				{
					CondTrigger condTrigger;
					if (objUs != null && lootUnit.strName == "[us]")
					{
						condTrigger = objUs.Clone();
					}
					else
					{
						condTrigger = DataHandler.GetCondTrigger(lootUnit.strName);
					}
					condTrigger.fCount *= num3;
					list.Add(condTrigger);
					break;
				}
			}
		}
		num = 0;
		foreach (List<LootUnit> list3 in this.aOtherLootUnits)
		{
			float num2 = 0f;
			string text2 = strRandID;
			if (text2 != null)
			{
				text2 += num;
			}
			num++;
			float fRand2 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text2);
			foreach (LootUnit lootUnit2 in list3)
			{
				float num4 = lootUnit2.GetAmount(num2, fRand2);
				if (!lootUnit2.bPositive)
				{
					num4 = -num4;
				}
				num2 += lootUnit2.fChance;
				if (num4 > 0f)
				{
					List<CondTrigger> ctloot = DataHandler.GetLoot(lootUnit2.strName).GetCTLoot(objUs, strRandID);
					foreach (CondTrigger condTrigger2 in ctloot)
					{
						condTrigger2.fCount *= num4;
					}
					list.AddRange(ctloot);
					break;
				}
			}
		}
		return list;
	}

	public List<CondOwner> GetCOLoot(CondOwner objUs, bool bSuppressOverride, string strRandID = null)
	{
		List<CondOwner> list = new List<CondOwner>();
		int num = 0;
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			float num2 = 0f;
			string text = strRandID;
			if (text != null)
			{
				text += num;
			}
			num++;
			float fRand = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text);
			foreach (LootUnit lootUnit in list2)
			{
				float num3 = lootUnit.GetAmount(num2, fRand);
				if (!lootUnit.bPositive)
				{
					num3 = -num3;
				}
				num2 += lootUnit.fChance;
				if (num3 > 0f)
				{
					int num4 = Mathf.FloorToInt(num3);
					for (int i = 0; i < num4; i++)
					{
						if (objUs != null && lootUnit.strName == "[us]")
						{
							list.Add(objUs);
						}
						else
						{
							list.Add(DataHandler.GetCondOwner(lootUnit.strName, null, null, !this.bSuppress || !bSuppressOverride, null, null, null, null));
						}
					}
					break;
				}
			}
		}
		num = 0;
		foreach (List<LootUnit> list3 in this.aOtherLootUnits)
		{
			float num2 = 0f;
			string text2 = strRandID;
			if (text2 != null)
			{
				text2 += num;
			}
			num++;
			float fRand2 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text2);
			foreach (LootUnit lootUnit2 in list3)
			{
				float num5 = lootUnit2.GetAmount(num2, fRand2);
				if (!lootUnit2.bPositive)
				{
					num5 = -num5;
				}
				num2 += lootUnit2.fChance;
				if (num5 > 0f)
				{
					int num6 = 0;
					while ((float)num6 < num5)
					{
						Loot loot = DataHandler.GetLoot(lootUnit2.strName);
						if (loot.strName != lootUnit2.strName)
						{
							Debug.Log(string.Concat(new string[]
							{
								this.strName,
								" expected loot: ",
								lootUnit2.strName,
								" but got ",
								loot.strName
							}));
						}
						list.AddRange(loot.GetCOLoot(objUs, this.bSuppress && bSuppressOverride, strRandID));
						num6++;
					}
					break;
				}
			}
		}
		if (this.bNested)
		{
			for (int j = list.Count - 1; j > 0; j--)
			{
				for (int k = j - 1; k >= 0; k--)
				{
					list[j] = list[k].AddCO(list[j], true, true, true);
					if (list[j] == null)
					{
						list.RemoveAt(j);
						break;
					}
				}
			}
		}
		return list;
	}

	public List<string> GetLootNames(string strRandID = null, bool bOnlyCOs = false, string type = null)
	{
		List<string> list = new List<string>();
		int num = 0;
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			if (!string.IsNullOrEmpty(type) && this.strType != type)
			{
				break;
			}
			float num2 = 0f;
			string text = strRandID;
			if (text != null)
			{
				text += num;
			}
			num++;
			float fRand = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text);
			foreach (LootUnit lootUnit in list2)
			{
				float num3 = lootUnit.GetAmount(num2, fRand);
				num2 += lootUnit.fChance;
				string text2 = lootUnit.strName;
				if (num3 < 0f)
				{
					text2 = "-" + text2;
					num3 = -num3;
				}
				if (num3 > 0f)
				{
					int num4 = 0;
					while ((float)num4 < num3)
					{
						list.Add(text2);
						num4++;
					}
					break;
				}
			}
		}
		if (bOnlyCOs)
		{
			return list;
		}
		num = 0;
		foreach (List<LootUnit> list3 in this.aOtherLootUnits)
		{
			float num2 = 0f;
			string text3 = strRandID;
			if (text3 != null)
			{
				text3 += num;
			}
			num++;
			float fRand2 = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text3);
			foreach (LootUnit lootUnit2 in list3)
			{
				float num5 = lootUnit2.GetAmount(num2, fRand2);
				num2 += lootUnit2.fChance;
				string str = lootUnit2.strName;
				if (num5 < 0f)
				{
					str = "-" + str;
					num5 = -num5;
				}
				if (num5 > 0f)
				{
					int num6 = 0;
					while ((float)num6 < num5)
					{
						Loot loot = DataHandler.GetLoot(lootUnit2.strName);
						if (string.IsNullOrEmpty(type) || !(loot.strType != type))
						{
							list.AddRange(loot.GetLootNames(text3, false, null));
						}
						num6++;
					}
					break;
				}
			}
		}
		return list;
	}

	public string GetLootNameSingle(string strRandID = null)
	{
		List<string> lootNames = this.GetLootNames(strRandID, false, null);
		if (lootNames.Count > 0)
		{
			return lootNames[0];
		}
		return null;
	}

	public List<string> GetAllLootNames()
	{
		List<string> list = new List<string>();
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			foreach (LootUnit lootUnit in list2)
			{
				list.Add(lootUnit.strName);
			}
		}
		foreach (List<LootUnit> list3 in this.aOtherLootUnits)
		{
			foreach (LootUnit lootUnit2 in list3)
			{
				list.AddRange(DataHandler.GetLoot(lootUnit2.strName).GetAllLootNames());
			}
		}
		return list;
	}

	public void ApplyCondLoot(CondOwner coUs, float fCoeff, string strRandID = null, float fCondRuleTrack = 0f)
	{
		if (coUs == null || this.strType != "condition")
		{
			return;
		}
		foreach (string strDef in this.aCOs)
		{
			string text = coUs.ParseCondEquation(strDef, (double)fCoeff, fCondRuleTrack);
		}
		int num = 0;
		foreach (List<LootUnit> list in this.aOtherLootUnits)
		{
			float num2 = 0f;
			string text2 = strRandID;
			if (text2 != null)
			{
				text2 += num;
			}
			num++;
			float fRand = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text2);
			foreach (LootUnit lootUnit in list)
			{
				float num3 = lootUnit.GetAmount(num2, fRand);
				if (!lootUnit.bPositive)
				{
					num3 = -num3;
				}
				num2 += lootUnit.fChance;
				if (num3 > 0f)
				{
					int num4 = 0;
					while ((float)num4 < num3)
					{
						Loot loot = DataHandler.GetLoot(lootUnit.strName);
						loot.ApplyCondLoot(coUs, fCoeff, null, fCondRuleTrack);
						num4++;
					}
					break;
				}
			}
		}
	}

	public static KeyValuePair<string, Tuple<double, double>> ParseCondEquation(string strDef)
	{
		if (strDef == null)
		{
			return new KeyValuePair<string, Tuple<double, double>>(string.Empty, new Tuple<double, double>(0.0, 0.0));
		}
		string[] array = strDef.Split(new char[]
		{
			'|'
		});
		float value = UnityEngine.Random.value;
		double num = 1.0;
		float num2 = 0f;
		foreach (string text in array)
		{
			string text2 = text;
			if (text[0] == '-')
			{
				num = -num;
				text2 = text.Substring(1);
			}
			string[] array3 = text2.Split(new char[]
			{
				'='
			});
			string key = array3[0];
			float num3 = 0f;
			double num4 = 0.0;
			double num5 = 0.0;
			if (array3.Length < 2)
			{
				Debug.Log("Missing CO Starting Cond Data: " + strDef);
				return new KeyValuePair<string, Tuple<double, double>>(string.Empty, new Tuple<double, double>(0.0, 0.0));
			}
			array3 = array3[1].Split(new char[]
			{
				'x'
			});
			float.TryParse(array3[0], out num3);
			if (num3 == 1f || value < num3 + num2)
			{
				bool flag = false;
				if (array3[1].IndexOf("E-") >= 0)
				{
					flag = true;
					array3[1] = array3[1].Replace("E-", "ee");
				}
				array3 = array3[1].Split(new char[]
				{
					'-'
				});
				if (flag)
				{
					array3[0] = array3[0].Replace("ee", "E-");
				}
				double.TryParse(array3[0], out num4);
				if (array3.Length != 1)
				{
					if (flag)
					{
						array3[1] = array3[1].Replace("ee", "E-");
					}
					double.TryParse(array3[1], out num5);
				}
				else
				{
					num5 = num4;
				}
				return new KeyValuePair<string, Tuple<double, double>>(key, new Tuple<double, double>(num4 * num, num5 * num));
			}
			num2 += num3;
		}
		return new KeyValuePair<string, Tuple<double, double>>(string.Empty, new Tuple<double, double>(0.0, 0.0));
	}

	public Dictionary<string, double> GetCondLoot(float fCoeff, Dictionary<string, double> dictOut, string strRandID = null)
	{
		if (dictOut == null)
		{
			dictOut = new Dictionary<string, double>();
		}
		if (this.strType != "condition")
		{
			return dictOut;
		}
		foreach (string strDef in this.aCOs)
		{
			KeyValuePair<string, Tuple<double, double>> keyValuePair = Loot.ParseCondEquation(strDef);
			double num = keyValuePair.Value.Item1;
			if (keyValuePair.Value.Item2 != num)
			{
				num = MathUtils.Rand(keyValuePair.Value.Item1, keyValuePair.Value.Item2, MathUtils.RandType.Flat, null);
			}
			if (keyValuePair.Key != string.Empty && num != 0.0)
			{
				if (!dictOut.ContainsKey(keyValuePair.Key))
				{
					dictOut[keyValuePair.Key] = num * (double)fCoeff;
				}
				else
				{
					Dictionary<string, double> dictionary;
					string key;
					(dictionary = dictOut)[key = keyValuePair.Key] = dictionary[key] + num * (double)fCoeff;
				}
			}
		}
		int num2 = 0;
		foreach (List<LootUnit> list in this.aOtherLootUnits)
		{
			float num3 = 0f;
			string text = strRandID;
			if (text != null)
			{
				text += num2;
			}
			num2++;
			float fRand = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, text);
			foreach (LootUnit lootUnit in list)
			{
				float num4 = lootUnit.GetAmount(num3, fRand);
				if (!lootUnit.bPositive)
				{
					num4 = -num4;
				}
				num3 += lootUnit.fChance;
				if (num4 > 0f)
				{
					int num5 = 0;
					while ((float)num5 < num4)
					{
						Loot loot = DataHandler.GetLoot(lootUnit.strName);
						loot.GetCondLoot(fCoeff, dictOut, null);
						num5++;
					}
					break;
				}
			}
		}
		return dictOut;
	}

	public Loot Clone()
	{
		return new Loot
		{
			strName = this.strName,
			strType = this.strType,
			bNested = this.bNested,
			bSuppress = this.bSuppress,
			aCOs = (string[])this.aCOs.Clone(),
			aLoots = (string[])this.aLoots.Clone()
		};
	}

	public Loot CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		Loot loot = this.Clone();
		loot.strName = this.strName.Replace(strFind, strReplace);
		bool flag = this.strType == "condition" || this.strType == "interaction" || this.strType == "trigger" || this.strType == "condrule";
		if (flag)
		{
			if (this.aCOs != null)
			{
				for (int i = 0; i < this.aCOs.Length; i++)
				{
					if (!string.IsNullOrEmpty(this.aCOs[i]))
					{
						string[] array = this.aCOs[i].Split(new char[]
						{
							'='
						});
						if (!string.IsNullOrEmpty(array[0]))
						{
							string text = array[0];
							bool flag2 = array[0].IndexOf("-") == 0;
							if (flag2)
							{
								text = text.Substring(1);
							}
							string text2 = this.strType;
							if (text2 != null)
							{
								if (!(text2 == "condition"))
								{
									if (!(text2 == "condrule"))
									{
										if (!(text2 == "interaction"))
										{
											if (text2 == "trigger")
											{
												text = CondTrigger.CloneDeep(text, strReplace, strFind);
											}
										}
										else
										{
											text = JsonInteraction.CloneDeep(text, strReplace, strFind);
										}
									}
									else
									{
										text = CondRule.CloneDeep(text, strReplace, strFind);
									}
								}
								else
								{
									text = JsonCond.CloneDeep(text, strReplace, strFind);
								}
							}
							if (flag2)
							{
								text = "-" + text;
							}
							loot.aCOs[i] = loot.aCOs[i].Replace(array[0], text);
						}
					}
				}
			}
			if (this.aLoots != null)
			{
				for (int j = 0; j < this.aLoots.Length; j++)
				{
					if (!string.IsNullOrEmpty(this.aLoots[j]))
					{
						string[] array2 = this.aLoots[j].Split(new char[]
						{
							','
						});
						if (!string.IsNullOrEmpty(array2[0]))
						{
							string text3 = array2[0];
							bool flag3 = array2[0].IndexOf("-") == 0;
							if (flag3)
							{
								text3 = text3.Substring(1);
							}
							text3 = Loot.CloneDeep(text3, strReplace, strFind);
							if (flag3)
							{
								text3 = "-" + text3;
							}
							loot.aLoots[j] = loot.aLoots[j].Replace(array2[0], text3);
						}
					}
				}
			}
		}
		DataHandler.dictLoot[loot.strName] = loot;
		return loot;
	}

	public static string CloneDeep(string strLoot, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strLoot) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strLoot.IndexOf(strFind) < 0)
		{
			return strLoot;
		}
		Loot loot = null;
		if (!DataHandler.dictLoot.TryGetValue(strLoot, out loot))
		{
			return strLoot;
		}
		string text = strLoot.Replace(strFind, strReplace);
		Loot loot2 = null;
		if (!DataHandler.dictLoot.TryGetValue(text, out loot2))
		{
			loot2 = loot.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public IDictionary<string, IEnumerable> GetVerifiables()
	{
		Dictionary<string, IEnumerable> dictionary = new Dictionary<string, IEnumerable>();
		Type[] value = new Type[]
		{
			typeof(JsonCondOwner),
			typeof(JsonCOOverlay)
		};
		bool flag = false;
		string text = this.strType;
		switch (text)
		{
		case "condition":
			value = new Type[]
			{
				typeof(JsonCond)
			};
			flag = true;
			break;
		case "interaction":
			value = new Type[]
			{
				typeof(JsonInteraction)
			};
			break;
		case "relationship":
			return dictionary;
		case "trigger":
			value = new Type[]
			{
				typeof(CondTrigger)
			};
			break;
		case "lifeevent":
			value = new Type[]
			{
				typeof(JsonLifeEvent)
			};
			break;
		case "ship":
			value = new Type[]
			{
				typeof(JsonShip)
			};
			break;
		case "text":
			return dictionary;
		}
		if (this.aCOLootUnits != null && this.aCOLootUnits.Count > 0)
		{
			foreach (List<LootUnit> list in this.aCOLootUnits)
			{
				foreach (LootUnit lootUnit in list)
				{
					if (lootUnit != null && !string.IsNullOrEmpty(lootUnit.strName))
					{
						if (flag && lootUnit.strName.IndexOf("Thresh") == 0)
						{
							string text2 = lootUnit.strName;
							string key = null;
							text2 = text2.Substring(6);
							if (DataHandler.dictCondRulesLookup.TryGetValue(text2, out key))
							{
								dictionary.TryAdd(key, new Type[]
								{
									typeof(CondRule)
								});
							}
							else
							{
								dictionary.TryAdd(lootUnit.strName, value);
							}
						}
						else
						{
							dictionary.TryAdd(lootUnit.strName, value);
						}
					}
				}
			}
		}
		if (this.aOtherLootUnits != null && this.aOtherLootUnits.Count > 0)
		{
			foreach (List<LootUnit> list2 in this.aOtherLootUnits)
			{
				foreach (LootUnit lootUnit2 in list2)
				{
					if (lootUnit2 != null && !string.IsNullOrEmpty(lootUnit2.strName))
					{
						dictionary.TryAdd(lootUnit2.strName, new Type[]
						{
							typeof(Loot)
						});
					}
				}
			}
		}
		return dictionary;
	}

	public string strName
	{
		get
		{
			return this._strName;
		}
		set
		{
			this._strName = value;
		}
	}

	public string[] aCOs
	{
		get
		{
			return this._aCOs;
		}
		set
		{
			this._aCOs = value;
			this.aCOLootUnits = this.ParseLootDef(this._aCOs);
		}
	}

	public string[] aLoots
	{
		get
		{
			return this._aLoots;
		}
		set
		{
			this._aLoots = value;
			this.aOtherLootUnits = this.ParseLootDef(this._aLoots);
		}
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			this.strName,
			":",
			this._aCOs.Length,
			"+",
			this._aLoots.Length
		});
	}

	public const string NAME_BLANK = "Blank";

	public const string TYPE_TRIGGER = "trigger";

	public const string TYPE_COND = "condition";

	public const string TYPE_CONDRULE = "condrule";

	public const string TYPE_ITEM = "item";

	public const string TYPE_INTERACTION = "interaction";

	public const string TYPE_TEXT = "text";

	public const string TYPE_REL = "relationship";

	public const string TYPE_LIFEEVENT = "lifeevent";

	public const string TYPE_SHIP = "ship";

	public const string TYPE_Data = "data";

	public string strType;

	public bool bSuppress;

	public bool bNested;

	private string _strName;

	private string[] _aCOs;

	private string[] _aLoots;

	private List<List<LootUnit>> aCOLootUnits;

	private List<List<LootUnit>> aOtherLootUnits;

	private static readonly string[] _aDefault = new string[0];

	private static readonly List<List<LootUnit>> _aLUsDefault = new List<List<LootUnit>>();
}
