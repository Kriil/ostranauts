using System;
using System.Collections.Generic;
using Ostranauts.Electrical;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIBreaker : GUIData
{
	public Electrical Electrical
	{
		get
		{
			if (this.electrical == null)
			{
				this.electrical = this.COSelf.Electrical;
			}
			return this.electrical;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		Texture2D texture2D = DataHandler.LoadPNG("blank.png", false, false);
		this.dictPropMap = new Dictionary<string, string>
		{
			{
				"strGUIPrefab",
				"GUIBreaker"
			},
			{
				"strTitle",
				"Breaker"
			},
			{
				"strValidCOTrigger01",
				string.Empty
			},
			{
				"bMainBreakerStatus",
				"true"
			}
		};
		AudioManager.AddBtnAudio(this.mainBreaker.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
	}

	private void Update()
	{
		if (this.isInitiating)
		{
			return;
		}
		this.checkTime += Time.deltaTime;
		if (this.checkDuration > this.checkTime)
		{
			return;
		}
		this.checkTime = 0f;
		if (this.COSelf != null && this.COSelf.HasCond("IsPowered") && !this.COSelf.HasCond("IsSignalOff"))
		{
			this.imgLED.sprite = this.litLED;
		}
		else
		{
			this.imgLED.sprite = this.unlitLED;
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.electrical = coSelf.Electrical;
		this.mainBreaker.GetComponent<GUIToggleSwap>().OnTargetToggleValueChanged(this.electrical.GetOverride());
		this.mainBreaker.isOn = this.electrical.GetOverride();
		this.strValidCOTrigger01 = this.dictPropMap["strValidCOTrigger01"];
		foreach (ElectricalConnection connection in this.electrical.GetConnections())
		{
			GUIOutput component = UnityEngine.Object.Instantiate<GameObject>(this.outputPrefab, this.outputContainer).GetComponent<GUIOutput>();
			component.breaker = this;
			component.LoadFromConnection(connection, this.strCoSelfID);
			this.outputs.Add(component);
		}
		this.knobGate.State = (int)this.electrical.GateMode;
		this.knobGate.Callback = new Action<int>(this.electrical.SetGateMode);
		this.electrical.SliderCallback = new Action<float>(this.SetSlider);
		this.sliderDelay.value = this.electrical.DelaySlider;
		this.sliderDelay.onValueChanged.AddListener(new UnityAction<float>(this.electrical.SetSlider));
		this.isInitiating = false;
	}

	public override void SetInput(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		if (co.Electrical == null)
		{
			return;
		}
		base.SetInput(co);
		ElectricalConnection connection = this.Electrical.SetUpConnection(co);
		foreach (GUIOutput guioutput in this.outputs)
		{
			if (guioutput.destinationID == co.strID)
			{
				return;
			}
		}
		GUIOutput component = UnityEngine.Object.Instantiate<GameObject>(this.outputPrefab, this.outputContainer).GetComponent<GUIOutput>();
		component.breaker = this;
		component.LoadFromConnection(connection, this.strCoSelfID);
		this.outputs.Add(component);
	}

	public void AddInput()
	{
		CrewSim.bJustClickedInput = true;
		CrewSim.objInstance.ShowInputSelector(DataHandler.GetCondTrigger(this.strValidCOTrigger01), this);
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SaveAndClose();
	}

	public void ToggleMain()
	{
		if (this.isInitiating)
		{
			return;
		}
		this.Electrical.ToggleOverride();
	}

	public void SetSlider(float amt)
	{
		if (this.sliderTxt == null)
		{
			return;
		}
		this.sliderTxt.text = amt.ToString("N1") + "s";
	}

	public Toggle mainBreaker;

	public GUIKnob knobGate;

	public Slider sliderDelay;

	public TextMeshProUGUI sliderTxt;

	public RectTransform outputContainer;

	public GameObject outputPrefab;

	public List<GUIOutput> outputs;

	public Image imgLED;

	public Sprite litLED;

	public Sprite unlitLED;

	public bool isInitiating = true;

	private Electrical electrical;

	private string strValidCOTrigger01;

	private float checkDuration = 1f;

	private float checkTime;
}
