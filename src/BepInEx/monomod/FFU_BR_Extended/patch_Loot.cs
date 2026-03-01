using System;
using System.Collections.Generic;
using FFU_Beyond_Reach;
using MonoMod;
using Ostranauts.Core.Models;
using Ostranauts.Tools;
using UnityEngine;
// Extended loot parser/runtime for FFU_BR-specific data commands.
// Likely: this complements the core loader by honoring new array/object edit
// semantics and dynamic-range rules during loot resolution.
public class patch_Loot : Loot
{
	[MonoModReplace]
	public List<CondTrigger> GetCTLoot(CondTrigger objUs, string strRandID = null)
	{
		List<CondTrigger> list = new List<CondTrigger>();
		int num = 0;
		float num2 = 0f;
		float num3 = 1f;
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			num2 = 0f;
			string text = strRandID;
			bool flag = text != null;
			if (flag)
			{
				text += num.ToString();
			}
			num++;
			bool dynamicRandomRange = FFU_BR_Defs.DynamicRandomRange;
			if (dynamicRandomRange)
			{
				float num4 = 0f;
				foreach (LootUnit lootUnit in list2)
				{
					num4 += lootUnit.fChance;
				}
				num3 = ((num4 > 1f) ? num4 : 1f);
			}
			float num5 = MathUtils.Rand(0f, num3, 0, text);
			foreach (LootUnit lootUnit2 in list2)
			{
				float num6 = lootUnit2.GetAmount(num2, num5);
				bool flag2 = !lootUnit2.bPositive;
				if (flag2)
				{
					num6 = 0f - num6;
				}
				num2 += lootUnit2.fChance;
				bool flag3 = num6 > 0f;
				if (flag3)
				{
					CondTrigger condTrigger = (objUs == null || !(lootUnit2.strName == "[us]")) ? DataHandler.GetCondTrigger(lootUnit2.strName) : objUs.Clone();
					condTrigger.fCount *= num6;
					list.Add(condTrigger);
					break;
				}
			}
		}
		num = 0;
		num3 = 1f;
		foreach (List<LootUnit> list3 in this.aOtherLootUnits)
		{
			num2 = 0f;
			string text2 = strRandID;
			bool flag4 = text2 != null;
			if (flag4)
			{
				text2 += num.ToString();
			}
			num++;
			bool dynamicRandomRange2 = FFU_BR_Defs.DynamicRandomRange;
			if (dynamicRandomRange2)
			{
				float num7 = 0f;
				foreach (LootUnit lootUnit3 in list3)
				{
					num7 += lootUnit3.fChance;
				}
				num3 = ((num7 > 1f) ? num7 : 1f);
			}
			float num8 = MathUtils.Rand(0f, num3, 0, text2);
			foreach (LootUnit lootUnit4 in list3)
			{
				float num9 = lootUnit4.GetAmount(num2, num8);
				bool flag5 = !lootUnit4.bPositive;
				if (flag5)
				{
					num9 = 0f - num9;
				}
				num2 += lootUnit4.fChance;
				bool flag6 = num9 <= 0f;
				if (!flag6)
				{
					List<CondTrigger> ctloot = DataHandler.GetLoot(lootUnit4.strName).GetCTLoot(objUs, strRandID);
					foreach (CondTrigger condTrigger2 in ctloot)
					{
						condTrigger2.fCount *= num9;
					}
					list.AddRange(ctloot);
					break;
				}
			}
		}
		return list;
	}
	[MonoModReplace]
	public List<CondOwner> GetCOLoot(CondOwner objUs, bool bSuppressOverride, string strRandID = null)
	{
		List<CondOwner> list = new List<CondOwner>();
		int num = 0;
		float num2 = 0f;
		float num3 = 1f;
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			num2 = 0f;
			string text = strRandID;
			bool flag = text != null;
			if (flag)
			{
				text += num.ToString();
			}
			num++;
			bool dynamicRandomRange = FFU_BR_Defs.DynamicRandomRange;
			if (dynamicRandomRange)
			{
				float num4 = 0f;
				foreach (LootUnit lootUnit in list2)
				{
					num4 += lootUnit.fChance;
				}
				num3 = ((num4 > 1f) ? num4 : 1f);
			}
			float num5 = MathUtils.Rand(0f, num3, 0, text);
			foreach (LootUnit lootUnit2 in list2)
			{
				float num6 = lootUnit2.GetAmount(num2, num5);
				bool flag2 = !lootUnit2.bPositive;
				if (flag2)
				{
					num6 = 0f - num6;
				}
				num2 += lootUnit2.fChance;
				bool flag3 = num6 <= 0f;
				if (!flag3)
				{
					int num7 = Mathf.FloorToInt(num6);
					for (int i = 0; i < num7; i++)
					{
						bool flag4 = objUs != null && lootUnit2.strName == "[us]";
						if (flag4)
						{
							list.Add(objUs);
						}
						else
						{
							list.Add(DataHandler.GetCondOwner(lootUnit2.strName, null, null, !this.bSuppress || !bSuppressOverride, null, null, null, null));
						}
					}
					break;
				}
			}
		}
		num = 0;
		num3 = 1f;
		foreach (List<LootUnit> list3 in this.aOtherLootUnits)
		{
			num2 = 0f;
			string text2 = strRandID;
			bool flag5 = text2 != null;
			if (flag5)
			{
				text2 += num.ToString();
			}
			num++;
			bool dynamicRandomRange2 = FFU_BR_Defs.DynamicRandomRange;
			if (dynamicRandomRange2)
			{
				float num8 = 0f;
				foreach (LootUnit lootUnit3 in list3)
				{
					num8 += lootUnit3.fChance;
				}
				num3 = ((num8 > 1f) ? num8 : 1f);
			}
			float num9 = MathUtils.Rand(0f, num3, 0, text2);
			foreach (LootUnit lootUnit4 in list3)
			{
				float num10 = lootUnit4.GetAmount(num2, num9);
				bool flag6 = !lootUnit4.bPositive;
				if (flag6)
				{
					num10 = 0f - num10;
				}
				num2 += lootUnit4.fChance;
				bool flag7 = num10 <= 0f;
				if (!flag7)
				{
					int num11 = 0;
					while ((float)num11 < num10)
					{
						Loot loot = DataHandler.GetLoot(lootUnit4.strName);
						bool flag8 = loot.strName != lootUnit4.strName;
						if (flag8)
						{
							Debug.Log(string.Concat(new string[]
							{
								base.strName,
								" expected loot: ",
								lootUnit4.strName,
								" but got ",
								loot.strName
							}));
						}
						list.AddRange(loot.GetCOLoot(objUs, this.bSuppress && bSuppressOverride, strRandID));
						num11++;
					}
					break;
				}
			}
		}
		bool bNested = this.bNested;
		if (bNested)
		{
			for (int j = list.Count - 1; j > 0; j--)
			{
				for (int k = j - 1; k >= 0; k--)
				{
					list[j] = list[k].AddCO(list[j], true, true, true);
					bool flag9 = list[j] == null;
					if (flag9)
					{
						list.RemoveAt(j);
						break;
					}
				}
			}
		}
		return list;
	}
	[MonoModReplace]
	public List<string> GetLootNames(string strRandID = null, bool bOnlyCOs = false, string type = null)
	{
		List<string> list = new List<string>();
		int num = 0;
		float num2 = 0f;
		float num3 = 1f;
		foreach (List<LootUnit> list2 in this.aCOLootUnits)
		{
			bool flag = !string.IsNullOrEmpty(type) && this.strType != type;
			if (flag)
			{
				break;
			}
			num2 = 0f;
			string text = strRandID;
			bool flag2 = text != null;
			if (flag2)
			{
				text += num.ToString();
			}
			num++;
			bool dynamicRandomRange = FFU_BR_Defs.DynamicRandomRange;
			if (dynamicRandomRange)
			{
				float num4 = 0f;
				foreach (LootUnit lootUnit in list2)
				{
					num4 += lootUnit.fChance;
				}
				num3 = ((num4 > 1f) ? num4 : 1f);
			}
			float num5 = MathUtils.Rand(0f, num3, 0, text);
			foreach (LootUnit lootUnit2 in list2)
			{
				float num6 = lootUnit2.GetAmount(num2, num5);
				num2 += lootUnit2.fChance;
				string text2 = lootUnit2.strName;
				bool flag3 = num6 < 0f;
				if (flag3)
				{
					text2 = "-" + text2;
					num6 = 0f - num6;
				}
				bool flag4 = num6 > 0f;
				if (flag4)
				{
					int num7 = 0;
					while ((float)num7 < num6)
					{
						list.Add(text2);
						num7++;
					}
					break;
				}
			}
		}
		List<string> result;
		if (bOnlyCOs)
		{
			result = list;
		}
		else
		{
			num = 0;
			num3 = 1f;
			foreach (List<LootUnit> list3 in this.aOtherLootUnits)
			{
				num2 = 0f;
				string text3 = strRandID;
				bool flag5 = text3 != null;
				if (flag5)
				{
					text3 += num.ToString();
				}
				num++;
				bool dynamicRandomRange2 = FFU_BR_Defs.DynamicRandomRange;
				if (dynamicRandomRange2)
				{
					float num8 = 0f;
					foreach (LootUnit lootUnit3 in list3)
					{
						num8 += lootUnit3.fChance;
					}
					num3 = ((num8 > 1f) ? num8 : 1f);
				}
				float num9 = MathUtils.Rand(0f, num3, 0, text3);
				foreach (LootUnit lootUnit4 in list3)
				{
					float num10 = lootUnit4.GetAmount(num2, num9);
					num2 += lootUnit4.fChance;
					string str = lootUnit4.strName;
					bool flag6 = num10 < 0f;
					if (flag6)
					{
						str = "-" + str;
						num10 = 0f - num10;
					}
					bool flag7 = num10 <= 0f;
					if (!flag7)
					{
						int num11 = 0;
						while ((float)num11 < num10)
						{
							Loot loot = DataHandler.GetLoot(lootUnit4.strName);
							bool flag8 = string.IsNullOrEmpty(type) || !(loot.strType != type);
							if (flag8)
							{
								list.AddRange(loot.GetLootNames(text3, false, null));
							}
							num11++;
						}
						break;
					}
				}
			}
			result = list;
		}
		return result;
	}
	[MonoModReplace]
	public void ApplyCondLoot(CondOwner coUs, float fCoeff, string strRandID = null, float fCondRuleTrack = 0f)
	{
		bool flag = coUs == null || this.strType != "condition";
		if (!flag)
		{
			string[] aCOs = base.aCOs;
			foreach (string text in aCOs)
			{
				coUs.ParseCondEquation(text, (double)fCoeff, fCondRuleTrack);
			}
			int num = 0;
			float num2 = 1f;
			foreach (List<LootUnit> list in this.aOtherLootUnits)
			{
				float num3 = 0f;
				string text2 = strRandID;
				bool flag2 = text2 != null;
				if (flag2)
				{
					text2 += num.ToString();
				}
				num++;
				bool dynamicRandomRange = FFU_BR_Defs.DynamicRandomRange;
				if (dynamicRandomRange)
				{
					float num4 = 0f;
					foreach (LootUnit lootUnit in list)
					{
						num4 += lootUnit.fChance;
					}
					num2 = ((num4 > 1f) ? num4 : 1f);
				}
				float num5 = MathUtils.Rand(0f, num2, 0, text2);
				foreach (LootUnit lootUnit2 in list)
				{
					float num6 = lootUnit2.GetAmount(num3, num5);
					bool flag3 = !lootUnit2.bPositive;
					if (flag3)
					{
						num6 = 0f - num6;
					}
					num3 += lootUnit2.fChance;
					bool flag4 = num6 > 0f;
					if (flag4)
					{
						int num7 = 0;
						while ((float)num7 < num6)
						{
							Loot loot = DataHandler.GetLoot(lootUnit2.strName);
							loot.ApplyCondLoot(coUs, fCoeff, null, fCondRuleTrack);
							num7++;
						}
						break;
					}
				}
			}
		}
	}
	[MonoModReplace]
	public Dictionary<string, double> GetCondLoot(float fCoeff, Dictionary<string, double> dictOut, string strRandID = null)
	{
		if (dictOut == null)
		{
			dictOut = new Dictionary<string, double>();
		}
		bool flag = this.strType != "condition";
		Dictionary<string, double> result;
		if (flag)
		{
			result = dictOut;
		}
		else
		{
			string[] aCOs = base.aCOs;
			foreach (string text in aCOs)
			{
				KeyValuePair<string, Tuple<double, double>> keyValuePair = Loot.ParseCondEquation(text);
				double num = keyValuePair.Value.Item1;
				bool flag2 = keyValuePair.Value.Item2 != num;
				if (flag2)
				{
					num = MathUtils.Rand(keyValuePair.Value.Item1, keyValuePair.Value.Item2, 0, null);
				}
				bool flag3 = keyValuePair.Key != string.Empty && num != 0.0;
				if (flag3)
				{
					bool flag4 = !dictOut.ContainsKey(keyValuePair.Key);
					if (flag4)
					{
						dictOut[keyValuePair.Key] = num * (double)fCoeff;
					}
					else
					{
						Dictionary<string, double> dictionary = dictOut;
						string key = keyValuePair.Key;
						dictionary[key] += num * (double)fCoeff;
					}
				}
			}
			int num2 = 0;
			float num3 = 1f;
			foreach (List<LootUnit> list in this.aOtherLootUnits)
			{
				float num4 = 0f;
				string text2 = strRandID;
				bool flag5 = text2 != null;
				if (flag5)
				{
					text2 += num2.ToString();
				}
				num2++;
				bool dynamicRandomRange = FFU_BR_Defs.DynamicRandomRange;
				if (dynamicRandomRange)
				{
					float num5 = 0f;
					foreach (LootUnit lootUnit in list)
					{
						num5 += lootUnit.fChance;
					}
					num3 = ((num5 > 1f) ? num5 : 1f);
				}
				float num6 = MathUtils.Rand(0f, num3, 0, text2);
				foreach (LootUnit lootUnit2 in list)
				{
					float num7 = lootUnit2.GetAmount(num4, num6);
					bool flag6 = !lootUnit2.bPositive;
					if (flag6)
					{
						num7 = 0f - num7;
					}
					num4 += lootUnit2.fChance;
					bool flag7 = num7 > 0f;
					if (flag7)
					{
						int num8 = 0;
						while ((float)num8 < num7)
						{
							Loot loot = DataHandler.GetLoot(lootUnit2.strName);
							loot.GetCondLoot(fCoeff, dictOut, null);
							num8++;
						}
						break;
					}
				}
			}
			result = dictOut;
		}
		return result;
	}
	[MonoModReplace]
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
				bool flag = text[0] == '-';
				if (flag)
				{
					lootUnit.bPositive = false;
					text3 = text2.Substring(1);
				}
				string[] array3 = text3.Split(new char[]
				{
					'='
				});
				lootUnit.strName = array3[0];
				bool flag2 = array3.Length < 2;
				if (flag2)
				{
					bool flag3 = FFU_BR_Defs.SyncLogging >= FFU_BR_Defs.SyncLogs.ModChanges;
					if (flag3)
					{
						Debug.Log("#Info# Loot entry '" + base.strName + "' is for patching only and not saved as permanent data.");
					}
					return new List<List<LootUnit>>();
				}
				array3 = array3[1].Split(new char[]
				{
					'x'
				});
				float.TryParse(array3[0], out lootUnit.fChance);
				bool flag4 = lootUnit.fChance < 0f;
				if (flag4)
				{
					JsonLogger.ReportProblem(string.Concat(new string[]
					{
						"[",
						base.strName,
						"] ",
						text2,
						" (loot definition chance can't be negative)"
					}), 1);
				}
				else
				{
					bool flag5 = array3.Length < 2;
					if (flag5)
					{
						JsonLogger.ReportProblem(string.Concat(new string[]
						{
							"[",
							base.strName,
							"] ",
							text2,
							" (loot definition is shorter than expected)"
						}), 1);
					}
					else
					{
						float num = 0f;
						bool flag6 = array3[1].StartsWith("-");
						if (flag6)
						{
							JsonLogger.ReportProblem(string.Concat(new string[]
							{
								"[",
								base.strName,
								"] ",
								text2,
								" (loot definition base value can't be negative)"
							}), 1);
						}
						else
						{
							array3 = array3[1].Split(new char[]
							{
								'-'
							});
							bool flag7 = float.TryParse(array3[0], out num);
							if (flag7)
							{
								lootUnit.fMin = num;
							}
							bool flag8 = array3.Length > 1;
							if (flag8)
							{
								num = 0f;
								bool flag9 = array3.Length > 2;
								if (flag9)
								{
									JsonLogger.ReportProblem(string.Concat(new string[]
									{
										"[",
										base.strName,
										"] ",
										text2,
										" (loot definition value is longer than expected)"
									}), 1);
									goto IL_2DC;
								}
								bool flag10 = float.TryParse(array3[1], out num);
								if (flag10)
								{
									lootUnit.fMax = num;
								}
							}
							bool flag11 = lootUnit.fMax < lootUnit.fMin;
							if (flag11)
							{
								lootUnit.fMax = lootUnit.fMin;
							}
							list2.Add(lootUnit);
						}
					}
				}
				IL_2DC:;
			}
			list.Add(list2);
		}
		return list;
	}
}
