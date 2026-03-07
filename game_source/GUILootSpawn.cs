using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUILootSpawn : GUIData
{
	private void PageSetup()
	{
		string text = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strType", out text))
		{
			text = "Loot";
		}
		List<string> list = new List<string>
		{
			"Loot",
			"Pspec",
			"Pspec Loot"
		};
		this.ddlType.ClearOptions();
		this.ddlType.AddOptions(list);
		this.ddlType.onValueChanged.AddListener(delegate(int A_1)
		{
			this.tBoxFilter.text = string.Empty;
			this.OnTypeChange("strType", this.ddlType.options[this.ddlType.value].text);
			this.rangeWarning.SetActive(this.IsOverWall());
		});
		this.ddlType.value = list.IndexOf(text);
		this.ddlType.RefreshShownValue();
		this.UpdateLootList(text);
		this.tBoxFilter.onValueChanged.AddListener(new UnityAction<string>(this.OnFilterChanged));
		this.tBoxFilter.onSelect.AddListener(delegate(string A_0)
		{
			CrewSim.StartTyping();
		});
		this.tBoxFilter.onDeselect.AddListener(delegate(string A_0)
		{
			CrewSim.EndTyping();
		});
		string text2 = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strCount", out text2))
		{
			text2 = "1";
		}
		TMP_InputField tboxCount = base.transform.Find("tboxCount").GetComponent<TMP_InputField>();
		tboxCount.onValueChanged.AddListener(delegate(string A_1)
		{
			this.OnValueChange("strCount", tboxCount.text);
		});
		tboxCount.text = text2;
		string item = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strRange", out item))
		{
			item = "0";
		}
		TMP_Dropdown ddlRange = base.transform.Find("ddlRange").GetComponent<TMP_Dropdown>();
		list.Clear();
		for (int i = 0; i < 20; i++)
		{
			list.Add(i.ToString());
		}
		ddlRange.ClearOptions();
		ddlRange.AddOptions(list);
		ddlRange.onValueChanged.AddListener(delegate(int A_1)
		{
			this.OnValueChange("strRange", ddlRange.options[ddlRange.value].text);
			this.rangeWarning.SetActive(this.IsOverWall());
		});
		ddlRange.value = list.IndexOf(item);
		ddlRange.RefreshShownValue();
		string value = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strNew", out value))
		{
			value = "False";
		}
		Toggle chkNew = base.transform.Find("chkNew").GetComponent<Toggle>();
		bool isOn = false;
		if (!bool.TryParse(value, out isOn))
		{
			isOn = false;
		}
		chkNew.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnValueChange("strNew", chkNew.isOn.ToString());
		});
		chkNew.isOn = isOn;
		string value2 = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strDamaged", out value2))
		{
			value2 = "False";
		}
		Toggle chkDamaged = base.transform.Find("chkDamaged").GetComponent<Toggle>();
		bool isOn2 = false;
		if (!bool.TryParse(value2, out isOn2))
		{
			isOn2 = false;
		}
		chkDamaged.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnValueChange("strDamaged", chkDamaged.isOn.ToString());
		});
		chkDamaged.isOn = isOn2;
		string value3 = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strDerelict", out value3))
		{
			value3 = "False";
		}
		Toggle chkDerelict = base.transform.Find("chkDerelict").GetComponent<Toggle>();
		bool isOn3 = false;
		if (!bool.TryParse(value3, out isOn3))
		{
			isOn3 = false;
		}
		chkDerelict.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnValueChange("strDerelict", chkDerelict.isOn.ToString());
		});
		chkDerelict.isOn = isOn3;
		this.rangeWarning.SetActive(this.IsOverWall());
	}

	private bool IsOverWall()
	{
		return this.ddlType.options[this.ddlType.value].text != "Loot" && this.COSelf.GetComponent<LootSpawner>().IsOverWall();
	}

	private void UpdateLootList(string strType)
	{
		string text = null;
		if (!this.COSelf.mapGUIPropMaps[this.strCOKey].TryGetValue("strLoot", out text))
		{
			text = "Blank";
		}
		TMP_Dropdown ddlLoot = base.transform.Find("ddlLoot").GetComponent<TMP_Dropdown>();
		ddlLoot.ClearOptions();
		if (GUILootSpawn.aLootList == null)
		{
			GUILootSpawn.aLootList = new List<string>();
			GUILootSpawn.aPspecList = new List<string>();
			GUILootSpawn.aPspecLootList = new List<string>();
			foreach (string text2 in DataHandler.dictLoot.Keys)
			{
				if (DataHandler.dictLoot[text2].strType == "item")
				{
					GUILootSpawn.aLootList.Add(text2);
				}
				else if (DataHandler.dictLoot[text2].strType == "pspec")
				{
					GUILootSpawn.aPspecLootList.Add(text2);
				}
			}
			foreach (string item in DataHandler.dictPersonSpecs.Keys)
			{
				GUILootSpawn.aPspecList.Add(item);
			}
			GUILootSpawn.aLootList.Sort();
			GUILootSpawn.aPspecLootList.Sort();
			GUILootSpawn.aPspecList.Sort();
		}
		List<string> list = null;
		if (strType == "Pspec")
		{
			list = new List<string>();
			if (!string.IsNullOrEmpty(this.filterTerm))
			{
				foreach (string text3 in GUILootSpawn.aPspecList)
				{
					if (!string.IsNullOrEmpty(text3))
					{
						if (text3.ToLower().Contains(this.filterTerm.ToLower()))
						{
							list.Add(text3);
						}
					}
				}
			}
			else
			{
				list = new List<string>(GUILootSpawn.aPspecList);
			}
		}
		else if (strType == "Pspec Loot")
		{
			list = new List<string>();
			if (!string.IsNullOrEmpty(this.filterTerm))
			{
				foreach (string text4 in GUILootSpawn.aPspecLootList)
				{
					if (!string.IsNullOrEmpty(text4))
					{
						if (text4.ToLower().Contains(this.filterTerm.ToLower()))
						{
							list.Add(text4);
						}
					}
				}
			}
			else
			{
				list = new List<string>(GUILootSpawn.aPspecLootList);
			}
		}
		else
		{
			list = new List<string>();
			if (!string.IsNullOrEmpty(this.filterTerm))
			{
				foreach (string text5 in GUILootSpawn.aLootList)
				{
					if (!string.IsNullOrEmpty(text5))
					{
						if (text5.ToLower().Contains(this.filterTerm.ToLower()))
						{
							list.Add(text5);
						}
					}
				}
			}
			else
			{
				list = new List<string>(GUILootSpawn.aLootList);
			}
		}
		ddlLoot.AddOptions(list);
		int num = list.IndexOf(text);
		if (num < 0 && list.Count > 0)
		{
			text = list[0];
		}
		ddlLoot.value = num;
		this.OnValueChange("strLoot", text);
		ddlLoot.RefreshShownValue();
		ddlLoot.onValueChanged.AddListener(delegate(int A_1)
		{
			this.OnValueChange("strLoot", ddlLoot.options[ddlLoot.value].text);
		});
	}

	private void OnFilterChanged(string filter)
	{
		this.filterTerm = filter;
		this.UpdateLootList(this.ddlType.options[this.ddlType.value].text);
	}

	private void OnValueChange(string strKey, string strValue)
	{
		this.COSelf.mapGUIPropMaps[this.strCOKey][strKey] = strValue;
		this.COSelf.GetComponent<LootSpawner>().UpdateAppearance();
	}

	private void OnTypeChange(string strKey, string strValue)
	{
		this.COSelf.mapGUIPropMaps[this.strCOKey][strKey] = strValue;
		this.COSelf.GetComponent<LootSpawner>().UpdateAppearance();
		this.UpdateLootList(strValue);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.PageSetup();
	}

	[SerializeField]
	private GameObject rangeWarning;

	[SerializeField]
	private TMP_InputField tBoxFilter;

	[SerializeField]
	private TMP_Dropdown ddlType;

	private static List<string> aLootList;

	private static List<string> aPspecList;

	private static List<string> aPspecLootList;

	private string filterTerm = string.Empty;
}
