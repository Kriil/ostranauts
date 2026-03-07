using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIRoster : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.tfContent = base.transform.Find("pnlList/scrollMask/pnlContent");
		this.txtTitleValue = base.transform.Find("txtTitleValue").GetComponent<TMP_Text>();
	}

	private void SetCrew()
	{
		if (this.COSelf == null || this.COSelf.Company == null)
		{
			return;
		}
		this.txtTitleValue.text = this.COSelf.Company.strName;
		GUIRosterRow original = Resources.Load<GUIRosterRow>("GUIShip/GUIRoster/GUIRosterRow");
		foreach (KeyValuePair<string, JsonCompanyRules> keyValuePair in this.COSelf.Company.mapRoster)
		{
			GUIRosterRow guirosterRow = UnityEngine.Object.Instantiate<GUIRosterRow>(original, this.tfContent);
			guirosterRow.SetOwner(keyValuePair.Key, keyValuePair.Value);
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		coSelf = CrewSim.coPlayer;
		base.Init(coSelf, dict, strCOKey);
		this._openZoneMenuButton.onClick.AddListener(delegate()
		{
			GUIActionKeySelector.commandToggleZoneUI.ExternalExecute();
		});
		this.SetCrew();
	}

	private Transform tfContent;

	private TMP_Text txtTitleValue;

	[SerializeField]
	private Button _openZoneMenuButton;
}
