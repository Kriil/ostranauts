using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIBattery : GUIData
{
	private void Start()
	{
		this._depletionString = DataHandler.GetString("GUI_BATTERY_DEPLETING", false);
		this._chargingString = DataHandler.GetString("GUI_BATTERY_CHARGING", false);
	}

	private void Update()
	{
		if (this.COSelf == null)
		{
			return;
		}
		Powered component = this.COSelf.GetComponent<Powered>();
		if (component == null)
		{
			return;
		}
		float num = (float)component.PowerStoredMax;
		float num2 = (float)this.COSelf.GetCondAmount("StatPower");
		this.sliderStatPower.value = ((num != 0f) ? (num2 / num) : 0f);
		string text = "-";
		bool flag = false;
		if (this.COSelf.mapInfo != null)
		{
			if (this.COSelf.mapInfo.TryGetValue("PowerCurrentLoad", out text))
			{
				this.txtLoad.text = text;
				flag = (!string.IsNullOrEmpty(text) && text[0] == '+');
				this.lblDepletionTime.text = ((!flag) ? this._depletionString : this._chargingString);
			}
			else
			{
				this.txtTime.text = "-";
			}
			if (this.COSelf.mapInfo.TryGetValue("PowerRemainingTime", out text))
			{
				this.txtTime.text = text;
			}
			else
			{
				this.txtTime.text = "-";
			}
		}
		else
		{
			this.txtTime.text = text;
			this.txtLoad.text = text;
		}
		this.arrowContainer.gameObject.SetActive(flag);
		this.arrowContainer.anchoredPosition = ((this.sliderStatPower.value >= 0.3f) ? new Vector2(this.arrowContainer.anchoredPosition.x, 0f) : new Vector2(this.arrowContainer.anchoredPosition.x, 24f));
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		if (coSelf != null)
		{
			this.lblTitle.text = coSelf.strNameFriendly;
		}
		else
		{
			this.lblTitle.text = string.Empty;
		}
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
	}

	[SerializeField]
	private TMP_Text lblTitle;

	[SerializeField]
	private TMP_Text txtLoad;

	[SerializeField]
	private TMP_Text txtTime;

	[SerializeField]
	private TMP_Text lblDepletionTime;

	[SerializeField]
	private Slider sliderStatPower;

	[SerializeField]
	private RectTransform arrowContainer;

	private string _depletionString;

	private string _chargingString;
}
