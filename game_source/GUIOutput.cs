using System;
using Ostranauts.Electrical;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIOutput : MonoBehaviour
{
	public Electrical Electrical
	{
		get
		{
			if (this.elec == null)
			{
				this.elec = this.breaker.Electrical;
			}
			return this.elec;
		}
	}

	private void Update()
	{
		if (!this.isInitialized)
		{
			return;
		}
		this.checkTime += Time.deltaTime;
		if (this.checkDuration > this.checkTime)
		{
			return;
		}
		this.checkTime = 0f;
		string strFileName = "missing.png";
		CondOwner cobyID = this.breaker.COSelf.ship.GetCOByID(this.destinationID);
		if (cobyID != null)
		{
			strFileName = cobyID.strPortraitImg + ".png";
		}
		Texture2D texture2D = DataHandler.LoadPNG(strFileName, false, false);
		this.img.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
		if (this.nickName == string.Empty && !CrewSim.Typing && cobyID != null)
		{
			this.label.text = cobyID.strNameFriendly;
		}
		if (cobyID != null && cobyID.HasCond("IsPowered") && !cobyID.HasCond("IsSignalOff") && !cobyID.HasCond("IsOverrideOff"))
		{
			this.led.sprite = this.litLED;
		}
		else
		{
			this.led.sprite = this.unlitLED;
		}
	}

	public void LoadFromConnection(ElectricalConnection connection, string self)
	{
		this.selfID = self;
		this.destinationID = connection.originID;
		this.toggle.isOn = connection.switchStatus;
		this.status = connection.switchStatus;
		this.toggle.isOn = this.status;
		this.toggle.GetComponent<GUIToggleSwap>().OnTargetToggleValueChanged(this.status);
		string strFileName = "missing.png";
		CondOwner cobyID = this.breaker.COSelf.ship.GetCOByID(this.destinationID);
		if (cobyID != null)
		{
			strFileName = cobyID.strPortraitImg + ".png";
		}
		Texture2D texture2D = DataHandler.LoadPNG(strFileName, false, false);
		this.img.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
		if (connection.nickName != null && connection.nickName.Length > 0)
		{
			this.label.text = connection.nickName;
			this.nickName = connection.nickName;
		}
		else if (cobyID != null)
		{
			this.label.text = cobyID.strNameFriendly;
			this.nickName = string.Empty;
		}
		this.label.onEndEdit.AddListener(new UnityAction<string>(this.ChangeName));
		if (cobyID != null && cobyID.HasCond("IsPowered") && !cobyID.HasCond("IsSignalOff") && !cobyID.HasCond("IsOverrideOff"))
		{
			this.led.sprite = this.litLED;
		}
		else
		{
			this.led.sprite = this.unlitLED;
		}
		AudioManager.AddBtnAudio(this.toggle.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
		this.isInitialized = true;
	}

	public void ToggleBreaker()
	{
		if (!this.isInitialized)
		{
			return;
		}
		this.Electrical.ToggleConnection(this.destinationID);
	}

	public void EjectFromBreaker()
	{
		if (!this.isInitialized)
		{
			return;
		}
		this.Electrical.RemoveConnection(this.destinationID);
		this.breaker.outputs.Remove(this);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void ChangeName(string name)
	{
		this.nickName = name;
		this.Electrical.RenameConnection(this.destinationID, this.nickName);
	}

	[Header("Parent Link")]
	public GUIBreaker breaker;

	public Electrical elec;

	[Header("Child Links")]
	public Toggle toggle;

	public Image img;

	public Image led;

	public TMP_InputField label;

	[Header("LEDs")]
	public Sprite unlitLED;

	public Sprite litLED;

	[Header("Data Settings")]
	public string selfID;

	public string destinationID;

	public bool status;

	private bool isInitialized;

	private float checkDuration = 0.1f;

	private float checkTime;

	private string nickName = string.Empty;
}
