using System;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using Ostranauts.ShipGUIs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIAirPump : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		GameObject original = Resources.Load<GameObject>("GUIShip/GUIInputScrew");
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.pnlInsideScrews);
		Text componentInChildren = gameObject.GetComponentInChildren<Text>();
		componentInChildren.text = "Input 1";
		this.pnlInsideScrews.parent.gameObject.SetActive(false);
		gameObject.GetComponentInChildren<Button>().onClick.AddListener(delegate()
		{
			this.ChooseInput("strInput01");
		});
		this.imgConduit01 = gameObject.transform.Find("bmp").GetComponent<Image>();
		Texture2D texture2D = DataHandler.LoadPNG("missing.png", false, false);
		this.imgConduit01.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
		this.imgConduit01.transform.Rotate(Vector3.forward, 90f);
		this.dictPropMap = new Dictionary<string, string>
		{
			{
				"strGUIPrefab",
				"GUIMonoInput"
			},
			{
				"strTitle",
				"Mono Input"
			},
			{
				"strValidCOTrigger01",
				string.Empty
			},
			{
				"strCondMonitor01",
				string.Empty
			},
			{
				"strInput01",
				string.Empty
			}
		};
		if (this.btnScrews != null)
		{
			foreach (Button button in this.btnScrews)
			{
				button.onClick.AddListener(new UnityAction(this.OpenPanel));
			}
		}
		this.btnDone.onClick.AddListener(new UnityAction(this.ClosePanel));
		this.knobBus.Callback = new Action<int>(this.SetPower);
		this.chkTurbo.onValueChanged.AddListener(new UnityAction<bool>(this.OnChangeTurbo));
		this.chkReverse.onValueChanged.AddListener(new UnityAction<bool>(this.OnChangeReverse));
		this.chkSlow.onValueChanged.AddListener(new UnityAction<bool>(this.OnChangeSlow));
	}

	private void SetPower(int nState)
	{
		this.COSelf.AddCondAmount("IsOverrideOff", -this.COSelf.GetCondAmount("IsOverrideOff"), 0.0, 0f);
		this.COSelf.AddCondAmount("IsOverrideOn", -this.COSelf.GetCondAmount("IsOverrideOn"), 0.0, 0f);
		if (nState != 1)
		{
			if (nState != 2)
			{
				this.COSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
			}
			else
			{
				this.COSelf.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
			}
		}
		base.SetPropMapData("nKnobBus", nState.ToString());
	}

	private void LoadCOStats()
	{
		if (this.COSelf == null)
		{
			Debug.LogError("GUIAirPump Cannot load null CO: " + this.strCoSelfID);
			return;
		}
		string text = null;
		bool flag = false;
		if (this.dictPropMap.TryGetValue("nKnobBus", out text) && text != string.Empty)
		{
			int state = this.knobBus.State;
			int.TryParse(text, out state);
			if (this.knobBus.State != state)
			{
				this.knobBus.State = state;
			}
			flag = true;
		}
		if (!flag)
		{
			if (this.COSelf.HasCond("IsOverrideOff"))
			{
				if (this.knobBus.State != 0)
				{
					this.knobBus.State = 0;
				}
			}
			else if (this.COSelf.HasCond("IsOverrideOn"))
			{
				if (this.knobBus.State != 2)
				{
					this.knobBus.State = 2;
				}
			}
			else if (this.knobBus.State != 1)
			{
				this.knobBus.State = 1;
			}
		}
		text = null;
		if (this.COSelf.HasCond("IsTurbo"))
		{
			CanvasManager.ShowCanvasGroup(this.cgTurbo);
			if (this.dictPropMap.TryGetValue("bTurbo", out text))
			{
				this.chkTurbo.isOn = bool.Parse(text);
			}
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgTurbo);
		}
		text = null;
		if (this.COSelf.HasCond("IsReverse"))
		{
			CanvasManager.ShowCanvasGroup(this.cgReverse);
			if (this.dictPropMap.TryGetValue("bReverse", out text))
			{
				this.chkReverse.isOn = bool.Parse(text);
			}
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgReverse);
		}
		if (this.COSelf.HasCond("IsSlowMode"))
		{
			CanvasManager.ShowCanvasGroup(this.cgSlow);
			if (this.dictPropMap.TryGetValue("bSlowMode", out text))
			{
				this.chkSlow.isOn = bool.Parse(text);
			}
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgSlow);
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.lblTitle.text = this.dictPropMap["strTitle"];
		this.strValidCOTrigger01 = this.dictPropMap["strValidCOTrigger01"];
		this.strCondMonitor01 = this.dictPropMap["strCondMonitor01"];
		this.strConnectModeInput = "strInput01";
		CondOwner cobyID = coSelf.ship.GetCOByID(this.dictPropMap["strInput01"]);
		this.SetInput(cobyID);
		this.LoadCOStats();
		this.SetupCanisters();
		this.UpdateSidePanel();
	}

	private void SetupCanisters()
	{
		if (this.COSelf == null)
		{
			return;
		}
		this._coUs = this.COSelf;
		this._coA = null;
		this._coB = null;
		this._coAName = null;
		this._coBName = null;
		GasPump component = this.COSelf.GetComponent<GasPump>();
		if (component == null)
		{
			return;
		}
		Tuple<CondOwner, CondOwner> actors = component.GetActors();
		if (actors != null)
		{
			this._coA = actors.Item1;
			this._coAName = ((!(this._coA == null)) ? this._coA.strID : null);
			this._coB = actors.Item2;
			this._coBName = ((!(this._coB == null)) ? this._coB.strID : null);
		}
	}

	private void Update()
	{
		if (StarSystem.fEpoch - this._lastUIUpdate < 1.0)
		{
			return;
		}
		this._lastUIUpdate = StarSystem.fEpoch;
		if (this.COSelf != this._coUs || (this._coA == null && this._coAName != null) || (this._coB == null && this._coBName != null))
		{
			this.SetupCanisters();
			this.UpdateSidePanel();
		}
	}

	private void UpdateSidePanel()
	{
		if (this._coA == null && this._coB == null)
		{
			this.sidePanel.SetActive(false);
		}
		else
		{
			this.sidePanel.SetActive(true);
		}
		if (this._coA != null && this._coA.HasCond("StatGasPressureMax"))
		{
			this.gaugeTop.gameObject.SetActive(true);
			this.pressurePanelTop.gameObject.SetActive(false);
			this.gaugeTop.SetData(this._coA, "StatGasPressureMax", "StatGasPressure", "kPa");
		}
		else
		{
			this.gaugeTop.gameObject.SetActive(false);
			this.pressurePanelTop.gameObject.SetActive(true);
			this.pressurePanelTop.SetData(this._coA);
		}
		if (this._coB != null && this._coB.HasCond("StatGasPressureMax"))
		{
			this.gaugeBottom.gameObject.SetActive(true);
			this.pressurePanelBottom.gameObject.SetActive(false);
			this.gaugeBottom.SetData(this._coB, "StatGasPressureMax", "StatGasPressure", "kPa");
		}
		else
		{
			this.gaugeBottom.gameObject.SetActive(false);
			this.pressurePanelBottom.gameObject.SetActive(true);
			this.pressurePanelBottom.SetData(this._coB);
		}
		if (this._coA == null && this._coB == null)
		{
			this.sidePanel.SetActive(false);
		}
		else
		{
			this.sidePanel.SetActive(true);
		}
		this.containerMiddle.rotation = Quaternion.Euler(0f, 0f, (float)((!this.chkReverse.isOn) ? 180 : 0));
	}

	public override void SetInput(CondOwner co)
	{
		base.SetInput(co);
		this.coTarget01 = co;
		if (this.coTarget01 == null)
		{
			this.coTarget01 = this.COSelf;
		}
		string strFileName;
		if (this.coTarget01 == this.COSelf)
		{
			strFileName = "blank.png";
		}
		else
		{
			strFileName = this.coTarget01.strPortraitImg + ".png";
		}
		Texture2D texture2D = DataHandler.LoadPNG(strFileName, false, false);
		this.imgConduit01.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
		this.dictPropMap[this.strConnectModeInput] = this.coTarget01.strID;
		this.COSelf.mapGUIPropMaps[this.strCOKey][this.strConnectModeInput] = this.coTarget01.strID;
		GasPump component = this.COSelf.GetComponent<GasPump>();
		if (component != null)
		{
			component.bUpdateRemote = true;
		}
		Heater component2 = this.COSelf.GetComponent<Heater>();
		if (component2 != null)
		{
			component2.bUpdateRemote = true;
		}
	}

	private void OnChangeTurbo(bool isOn)
	{
		if (isOn)
		{
			this.COSelf.SetCondAmount("IsTurboOn", 1.0, 0.0);
		}
		else
		{
			this.COSelf.ZeroCondAmount("IsTurboOn");
		}
		base.SetPropMapData("bTurbo", isOn.ToString().ToLower());
	}

	private void OnChangeSlow(bool isOn)
	{
		if (isOn)
		{
			this.COSelf.SetCondAmount("IsSlowModeOn", 1.0, 0.0);
		}
		else
		{
			this.COSelf.ZeroCondAmount("IsSlowModeOn");
		}
		base.SetPropMapData("bSlowMode", isOn.ToString().ToLower());
	}

	private void OnChangeReverse(bool isOn)
	{
		if (isOn)
		{
			this.COSelf.SetCondAmount("IsReverseOn", 1.0, 0.0);
		}
		else
		{
			this.COSelf.ZeroCondAmount("IsReverseOn");
		}
		base.SetPropMapData("bReverse", isOn.ToString().ToLower());
		this.UpdateSidePanel();
	}

	private void OpenPanel()
	{
		this.pnlInsideScrews.parent.gameObject.SetActive(true);
	}

	private void ClosePanel()
	{
		this.pnlInsideScrews.parent.gameObject.SetActive(false);
	}

	private void ChooseInput(string strInputName)
	{
		this.strConnectModeInput = strInputName;
		CrewSim.bJustClickedInput = true;
		CrewSim.objInstance.ShowInputSelector(DataHandler.GetCondTrigger(this.strValidCOTrigger01), this);
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SetPropMapData("bTurbo", this.chkTurbo.isOn.ToString().ToLower());
		base.SetPropMapData("bReverse", this.chkReverse.isOn.ToString().ToLower());
		base.SetPropMapData("bSlowMode", this.chkSlow.isOn.ToString().ToLower());
		base.SetPropMapData("nKnobBus", this.knobBus.State.ToString());
		base.SaveAndClose();
	}

	[Header("General")]
	[SerializeField]
	private TMP_Text lblTitle;

	[SerializeField]
	private Transform pnlInsideScrews;

	[SerializeField]
	private GUIKnob knobBus;

	[Header("Toggles")]
	[SerializeField]
	private CanvasGroup cgTurbo;

	[SerializeField]
	private CanvasGroup cgReverse;

	[SerializeField]
	private CanvasGroup cgSlow;

	[SerializeField]
	private Toggle chkTurbo;

	[SerializeField]
	private Toggle chkReverse;

	[SerializeField]
	private Toggle chkSlow;

	[Header("Buttons")]
	[SerializeField]
	private Button[] btnScrews;

	[SerializeField]
	private Button btnDone;

	[Header("SidePanel")]
	[SerializeField]
	private GameObject sidePanel;

	[SerializeField]
	private GUIGauge gaugeTop;

	[SerializeField]
	private GUIGauge gaugeBottom;

	[SerializeField]
	private RectTransform containerMiddle;

	[SerializeField]
	private GUIPressurePanel pressurePanelTop;

	[SerializeField]
	private GUIPressurePanel pressurePanelBottom;

	private CondOwner _coUs;

	private CondOwner _coA;

	private CondOwner _coB;

	private string _coAName;

	private string _coBName;

	private string strConnectModeInput;

	private CondOwner coTarget01;

	private string strValidCOTrigger01;

	private string strCondMonitor01;

	private Image imgConduit01;

	private double _lastUIUpdate;
}
