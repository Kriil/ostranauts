using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GUIOnOff : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.tfPanelIn = base.transform.Find("pnlInside/pnlInsideScrews");
		this.tfPanelIn.parent.gameObject.SetActive(false);
		this.lblTitle = base.transform.Find("lblTitle").GetComponent<TMP_Text>();
		this.knobBus = base.transform.Find("knob").GetComponent<GUIKnob>();
		this.knobBus.Callback = new Action<int>(this.SetPower);
	}

	private void SetPower(int nState)
	{
		this.COSelf.AddCondAmount("IsOverrideOff", -this.COSelf.GetCondAmount("IsOverrideOff"), 0.0, 0f);
		this.COSelf.AddCondAmount("IsOverrideOn", -this.COSelf.GetCondAmount("IsOverrideOn"), 0.0, 0f);
		if (nState != 1)
		{
			this.COSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
		}
		else
		{
			this.COSelf.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
		}
	}

	private void LoadCOStats()
	{
		if (this.COSelf == null)
		{
			Debug.LogError("GUIOnOff Cannot load null CO: " + this.strCoSelfID);
			return;
		}
		if (this.COSelf.HasCond("IsOverrideOff"))
		{
			if (this.knobBus.State != 0)
			{
				this.knobBus.State = 0;
			}
		}
		else if (this.knobBus.State != 1)
		{
			this.knobBus.State = 1;
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.lblTitle.text = this.dictPropMap["strTitle"];
		this.LoadCOStats();
	}

	public TMP_Text lblTitle;

	public Transform tfPanelIn;

	private string strConnectModeInput;

	private GUIKnob knobBus;
}
