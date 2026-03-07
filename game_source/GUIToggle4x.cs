using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIToggle4x : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.mapInputs = new Dictionary<string, GUIToggle4xInput>();
		base.transform.Find("btnScrew01").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("btnScrew02").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("btnScrew03").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("btnScrew04").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("pnlInside/btnDone").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.ClosePanel();
		});
		this.tfPanelIn = base.transform.Find("pnlInside/pnlInsideScrews");
		this.tfPanelIn.parent.gameObject.SetActive(false);
		this.nInputs = 8;
		this.dictPropMap = new Dictionary<string, string>
		{
			{
				"strGUIPrefab",
				"GUIToggle4x"
			}
		};
		GameObject original = Resources.Load<GameObject>("GUIShip/GUISpacer");
		for (int i = 0; i < this.nInputs; i++)
		{
			string strSuffix = (i + 1).ToString();
			string strSuffixNext = (i + 2).ToString();
			string str = (i / 2 + 1).ToString();
			string key = "strInput0" + strSuffix;
			string key2 = "strInput0" + strSuffix + "Interaction";
			string strTitleKey = "strTitle0" + str;
			string value3 = "INPUT " + str;
			string strChkKey = "bToggle0" + str;
			if (i % 2 == 0)
			{
				Text lbl = base.transform.Find("lblOn0" + str).GetComponent<Text>();
				InputField component = base.transform.Find("pnlInside/tboxInput0" + str).GetComponent<InputField>();
				component.onValueChanged.AddListener(delegate(string value)
				{
					this.ChangeLabel(lbl, strTitleKey, value);
				});
				Toggle component2 = base.transform.Find("chkInput0" + str).GetComponent<Toggle>();
				component2.onValueChanged.AddListener(delegate(bool value)
				{
					this.Activate(value, "strInput0" + strSuffix, "strInput0" + strSuffixNext, strChkKey);
				});
				if (i > 0)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original);
					gameObject.transform.SetParent(this.tfPanelIn);
				}
			}
			GUIToggle4xInput value2 = new GUIToggle4xInput(strSuffix, this.tfPanelIn, this);
			this.mapInputs[key] = value2;
			this.dictPropMap[key] = string.Empty;
			this.dictPropMap[key2] = string.Empty;
			this.dictPropMap[strTitleKey] = value3;
			this.dictPropMap[strChkKey] = "false";
		}
	}

	private void ChangeLabel(Text lbl, string strTitleKey, string strIn)
	{
		lbl.text = strIn;
		this.dictPropMap[strTitleKey] = strIn;
		this.COSelf.mapGUIPropMaps[this.strCOKey][strTitleKey] = strIn;
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		foreach (string text in this.mapInputs.Keys)
		{
			this.mapInputs[text].Init(coSelf, this.dictPropMap[text], this.dictPropMap[text + "Interaction"]);
		}
		for (int i = 0; i < this.nInputs; i++)
		{
			string str = (i / 2 + 1).ToString();
			string key = "strTitle0" + str;
			string key2 = "bToggle0" + str;
			if (i % 2 == 0)
			{
				Text component = base.transform.Find("lblOn0" + str).GetComponent<Text>();
				InputField component2 = base.transform.Find("pnlInside/tboxInput0" + str).GetComponent<InputField>();
				component.text = this.dictPropMap[key];
				component2.text = this.dictPropMap[key];
				Toggle component3 = base.transform.Find("chkInput0" + str).GetComponent<Toggle>();
				component3.isOn = (this.dictPropMap[key2] == "True");
			}
		}
	}

	public override void SetInput(CondOwner co)
	{
		base.SetInput(co);
	}

	private void OpenPanel()
	{
		this.tfPanelIn.parent.gameObject.SetActive(true);
	}

	private void ClosePanel()
	{
		this.tfPanelIn.parent.gameObject.SetActive(false);
	}

	private void Activate(bool bValue, string strInputOn, string strInputOff, string strChkKey)
	{
		if (bValue)
		{
			this.mapInputs[strInputOn].Activate(this.COSelf);
		}
		else
		{
			this.mapInputs[strInputOff].Activate(this.COSelf);
		}
		this.dictPropMap[strChkKey] = bValue.ToString();
		this.COSelf.mapGUIPropMaps[this.strCOKey][strChkKey] = bValue.ToString();
	}

	public Transform tfPanelIn;

	private int nInputs;

	public Dictionary<string, GUIToggle4xInput> mapInputs;
}
