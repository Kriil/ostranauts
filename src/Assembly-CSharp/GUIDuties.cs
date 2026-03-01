using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GUIDuties : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.tfContent = base.transform.Find("pnlList/scrollMask/pnlContent");
	}

	private void SetCrew()
	{
		if (this.COSelf != null && this.COSelf.Company != null)
		{
			TMP_Text component = base.transform.Find("txtTitleValue").GetComponent<TMP_Text>();
			component.text = this.COSelf.Company.strName;
			GUIDutiesRow original = Resources.Load<GUIDutiesRow>("GUIShip/GUIDuties/GUIDutiesRow");
			foreach (KeyValuePair<string, JsonCompanyRules> keyValuePair in this.COSelf.Company.mapRoster)
			{
				GUIDutiesRow guidutiesRow = UnityEngine.Object.Instantiate<GUIDutiesRow>(original, this.tfContent);
				guidutiesRow.SetOwner(keyValuePair.Key, keyValuePair.Value);
			}
		}
		Transform parent = base.transform.Find("pnlHeader/pnlItems");
		TMP_Text original2 = Resources.Load<TMP_Text>("GUIShip/GUIDuties/txtColumn");
		for (int i = 1; i < JsonCompanyRules.aDutiesNew.Length; i++)
		{
			string text = JsonCompanyRules.aDutiesNew[i];
			TMP_Text tmp_Text = UnityEngine.Object.Instantiate<TMP_Text>(original2, parent);
			tmp_Text.text = text;
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.SetCrew();
	}

	private Transform tfContent;
}
