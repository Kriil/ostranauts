using System;
using System.Collections.Generic;
using Ostranauts.Electrical;
using UnityEngine;

// Data-driven signal wiring component. This appears to read a GUI prop map for
// one installed device, then simulate logic-gate style signal routing between
// electrical connections on a ship.
public class Electrical : MonoBehaviour, IManUpdater
{
	// UI slider callback used by linked control widgets to push delay values back
	// into the electrical component.
	public Action<float> SliderCallback
	{
		get
		{
			return this.actSliderCallback;
		}
		set
		{
			this.actSliderCallback = value;
		}
	}

	// Main simulation tick: waits for the GUI prop map during setup, audits the
	// wiring periodically, and resolves queued signals on a short delay.
	private void Update()
	{
		if (this.isInitialising)
		{
			if (!this.CheckforGPM())
			{
				return;
			}
			this.fTimeToNextAudit = 1.0;
			this.fTimeOfNextSignalCheck = StarSystem.fEpoch + 0.1;
		}
		if (this.fTimeToNextAudit <= 0.0)
		{
			this.AuditConnections();
		}
		else
		{
			this.fTimeToNextAudit -= (double)Time.unscaledDeltaTime;
		}
		if (!this.bNeedsCheck || StarSystem.fEpoch < this.fTimeOfNextSignalCheck)
		{
			return;
		}
		this.ResolveSignalQueue();
	}

	// Manual driver used by manager code that updates IManUpdater objects.
	public void UpdateManual()
	{
		this.Update();
	}

	// Unclear: part of the manager interface, but currently unused.
	public void CatchUp()
	{
	}

	// Binds this component to its owning CondOwner and the named GUI prop map key
	// defined in the updater command list.
	public void SetData(string[] strings)
	{
		this.coSelf = base.gameObject.GetComponent<CondOwner>();
		this.coShip = this.coSelf.ship;
		if (this.coSelf == null)
		{
			Debug.LogWarning("Cannot set up electrical, no condowner on attached gameobject");
			return;
		}
		if (strings.Length <= 1)
		{
			Debug.LogWarning("Cannot set up electrical on " + this.coSelf.strNameFriendly + ". Not enough strings listed after \"Electrical\" in aUpdateCommands!");
			return;
		}
		this.mapGPM = null;
		this.strGPMKey = strings[1];
		this.CheckforGPM();
	}

	// Waits for the GUI prop map to exist on the CondOwner, then initializes the
	// electrical state from its serialized values.
	private bool CheckforGPM()
	{
		if (this.coSelf.mapGUIPropMaps.TryGetValue(this.strGPMKey, out this.mapGPM))
		{
			this.isInitialising = false;
			this.InitGPMValues();
			return true;
		}
		this.frameCount++;
		if (this.frameCount > 60)
		{
			this.RecoverBlank();
		}
		return false;
	}

	// Fallback when the runtime prop map never appeared: reloads a default GUI
	// prop map template from DataHandler.
	public void RecoverBlank()
	{
		this.mapGPM = DataHandler.GetGUIPropMap(this.strGPMKey);
		if (this.mapGPM != null)
		{
			this.coSelf.mapGUIPropMaps[this.strGPMKey] = this.mapGPM;
		}
		else
		{
			Debug.LogWarning(string.Concat(new object[]
			{
				"Cannot set up electrical on ",
				this.coSelf,
				". guiPropMap not found: ",
				this.strGPMKey
			}));
		}
	}

	// Rehydrates the electrical component from serialized GUI prop map values:
	// status flags, gate mode, delay slider, connections, and queued signals.
	private void InitGPMValues()
	{
		if (this.coShip == null)
		{
			this.coShip = this.coSelf.ship;
		}
		string text = null;
		if (this.mapGPM.TryGetValue("status", out text))
		{
			if (bool.Parse(text))
			{
				this.status = true;
			}
			else
			{
				this.status = false;
			}
		}
		if (!this.status && !this.coSelf.HasCond("IsSignalOff"))
		{
			this.coSelf.AddCondAmount("IsSignalOff", 1.0, 0.0, 0f);
		}
		text = null;
		if (this.mapGPM.TryGetValue("gate", out text))
		{
			this.gateMode = (GateMode)int.Parse(text);
		}
		text = null;
		if (this.mapGPM.TryGetValue("delay", out text))
		{
			this.sliderPercent = float.Parse(text);
		}
		text = null;
		if (this.mapGPM.TryGetValue("override", out text))
		{
			if (bool.Parse(text))
			{
				this.bOverride = true;
			}
			else
			{
				this.bOverride = false;
			}
		}
		if (!this.bOverride && !this.coSelf.HasCond("IsOverrideOff"))
		{
			this.coSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
		}
		text = null;
		if (this.mapGPM.TryGetValue("inputConnections", out text))
		{
			string[] array = text.Split(new char[]
			{
				','
			});
			foreach (string text2 in array)
			{
				if (!(text2 == string.Empty) && text2 != null)
				{
					ElectricalConnection value = ElectricalConnection.FromString(text2);
					if (!this.inputConnections.ContainsKey(value.originID))
					{
						this.inputConnections[value.originID] = value;
					}
				}
			}
		}
		text = null;
		if (this.mapGPM.TryGetValue("outputConnections", out text))
		{
			string[] array3 = text.Split(new char[]
			{
				','
			});
			foreach (string text3 in array3)
			{
				if (!(text3 == string.Empty) && text3 != null)
				{
					ElectricalConnection value2 = ElectricalConnection.FromString(text3);
					if (!this.outputConnections.ContainsKey(value2.originID))
					{
						this.outputConnections[value2.originID] = value2;
					}
				}
			}
		}
		text = null;
		if (this.mapGPM.TryGetValue("signalQueue", out text))
		{
			string[] array5 = text.Split(new char[]
			{
				','
			});
			foreach (string text4 in array5)
			{
				if (!(text4 == string.Empty) && text4 != null)
				{
					ElectricalSignal signal = ElectricalSignal.FromString(text4);
					this.AddSignal(signal);
				}
			}
			this.mapGPM["signalQueue"] = string.Empty;
		}
		text = null;
		if (this.mapGPM.TryGetValue("sendQueue", out text))
		{
			string[] array7 = text.Split(new char[]
			{
				','
			});
			foreach (string text5 in array7)
			{
				if (!(text5 == string.Empty) && text5 != null)
				{
					ElectricalSignal item = ElectricalSignal.FromString(text5);
					this.sendQueue.Add(item);
				}
			}
			this.mapGPM["sendQueue"] = string.Empty;
		}
	}

	public bool ResolveSignalQueue()
	{
		List<ElectricalSignal> list = new List<ElectricalSignal>();
		foreach (ElectricalSignal item in this.signalQueue)
		{
			if (StarSystem.fEpoch - item.epoch >= (double)this.GetSliderReal())
			{
				list.Add(item);
				switch (item.signalType)
				{
				case SignalType.Off:
					if (this.inputConnections.ContainsKey(item.originID))
					{
						ElectricalConnection value = this.inputConnections[item.originID];
						value.signalType = item.signalType;
						this.inputConnections[item.originID] = value;
					}
					break;
				case SignalType.On:
					if (this.inputConnections.ContainsKey(item.originID))
					{
						ElectricalConnection value2 = this.inputConnections[item.originID];
						value2.signalType = item.signalType;
						this.inputConnections[item.originID] = value2;
					}
					break;
				case SignalType.Connect:
					if (!this.inputConnections.ContainsKey(item.originID))
					{
						ElectricalConnection value3 = new ElectricalConnection(item.originID, item.signalType, string.Empty, true);
						this.inputConnections[item.originID] = value3;
					}
					break;
				case SignalType.Disconnect:
					if (this.inputConnections.ContainsKey(item.originID))
					{
						this.inputConnections.Remove(item.originID);
					}
					break;
				}
			}
		}
		foreach (ElectricalSignal item2 in list)
		{
			this.signalQueue.Remove(item2);
		}
		if (this.signalQueue.Count == 0)
		{
			this.bNeedsCheck = false;
		}
		else
		{
			this.fTimeOfNextSignalCheck = StarSystem.fEpoch + 0.1;
		}
		bool flag = this.status;
		if (this.inputConnections.Count == 0)
		{
			flag = true;
		}
		else
		{
			int num = 0;
			int num2 = 0;
			foreach (ElectricalConnection electricalConnection in this.inputConnections.Values)
			{
				if (electricalConnection.switchStatus)
				{
					num2++;
					if (electricalConnection.signalType == SignalType.On)
					{
						num++;
					}
				}
			}
			switch (this.gateMode)
			{
			case GateMode.OR:
				flag = (num > 0);
				break;
			case GateMode.AND:
				flag = (num >= num2);
				break;
			case GateMode.NOR:
				flag = (num <= 0);
				break;
			case GateMode.NAND:
				flag = (num < num2);
				break;
			}
		}
		if (flag != this.status)
		{
			this.status = flag;
			if (this.status)
			{
				this.PropagateSignal(SignalType.On);
				this.coSelf.ZeroCondAmount("IsSignalOff");
			}
			else
			{
				this.PropagateSignal(SignalType.Off);
				this.coSelf.AddCondAmount("IsSignalOff", 1.0, 0.0, 0f);
			}
		}
		if (this.sendQueue.Count > 0)
		{
			if (this.coSelf != null && this.coSelf.HasCond("IsPowered"))
			{
				foreach (ElectricalSignal electricalSignal in this.sendQueue)
				{
					this.SendSignal(electricalSignal.originID, new ElectricalSignal(this.coSelf.strID, electricalSignal.signalType, electricalSignal.callback));
				}
				this.sendQueue.Clear();
			}
			else
			{
				this.bNeedsCheck = true;
				this.fTimeOfNextSignalCheck = StarSystem.fEpoch + 0.1;
			}
		}
		this.SetGPM();
		return false;
	}

	private void PropagateSignal(SignalType signalType = SignalType.None)
	{
		List<string> list = new List<string>();
		foreach (ElectricalConnection electricalConnection in this.outputConnections.Values)
		{
			if (electricalConnection.switchStatus)
			{
				this.QueueSignal(electricalConnection.originID, signalType);
			}
		}
		foreach (string key in this.outputRemoves)
		{
			this.outputConnections.Remove(key);
		}
		this.outputRemoves.Clear();
	}

	public void AddSignal(ElectricalSignal signal)
	{
		this.bNeedsCheck = true;
		this.signalQueue.Add(signal);
		if (this.fTimeOfNextSignalCheck < StarSystem.fEpoch)
		{
			this.fTimeOfNextSignalCheck = StarSystem.fEpoch + 0.1;
		}
		string text = string.Empty;
		bool flag = false;
		foreach (ElectricalSignal electricalSignal in this.signalQueue)
		{
			if (flag)
			{
				text += ",";
			}
			else
			{
				flag = true;
			}
			text += electricalSignal.ToString();
		}
		this.mapGPM["signalQueue"] = text;
		this.coSelf.mapGUIPropMaps[this.strGPMKey] = this.mapGPM;
	}

	public bool SendSignal(string destination, ElectricalSignal signal)
	{
		ElectricalConnection electricalConnection;
		if (this.outputConnections.TryGetValue(destination, out electricalConnection))
		{
			CondOwner cobyID = this.coShip.GetCOByID(destination);
			if (cobyID != null)
			{
				Electrical electrical = cobyID.Electrical;
				if (electrical != null)
				{
					electrical.AddSignal(signal);
					return true;
				}
				this.outputRemoves.Add(destination);
			}
			else
			{
				this.outputRemoves.Add(destination);
			}
		}
		return false;
	}

	public void QueueSignal(string destination, SignalType signalType)
	{
		this.sendQueue.Add(new ElectricalSignal(destination, signalType, false));
		this.bNeedsCheck = true;
		string text = string.Empty;
		bool flag = false;
		foreach (ElectricalSignal electricalSignal in this.sendQueue)
		{
			if (flag)
			{
				text += ",";
			}
			else
			{
				flag = true;
			}
			text += electricalSignal.ToString();
		}
		this.mapGPM["sendQueue"] = text;
		this.coSelf.mapGUIPropMaps[this.strGPMKey] = this.mapGPM;
	}

	public void RenameConnection(string destination, string newName)
	{
		ConsoleToGUI.instance.LogInfo("Changed breaker name to: " + newName);
		ElectricalConnection value;
		if (this.outputConnections.TryGetValue(destination, out value))
		{
			value.nickName = newName;
			this.outputConnections[destination] = value;
			this.SetGPM();
		}
	}

	public bool ToggleConnection(string destination)
	{
		ElectricalConnection value;
		if (this.outputConnections.TryGetValue(destination, out value))
		{
			value.switchStatus = !value.switchStatus;
			this.outputConnections[destination] = value;
			this.QueueSignal(destination, (!value.switchStatus) ? SignalType.Off : SignalType.On);
		}
		this.SetGPM();
		return false;
	}

	public List<ElectricalConnection> GetConnections()
	{
		List<ElectricalConnection> list = new List<ElectricalConnection>();
		foreach (ElectricalConnection item in this.outputConnections.Values)
		{
			list.Add(item);
		}
		return list;
	}

	public void ToggleOverride()
	{
		this.bOverride = !this.bOverride;
		this.mapGPM["override"] = this.bOverride.ToString().ToLower();
		this.coSelf.mapGUIPropMaps[this.strGPMKey] = this.mapGPM;
		this.PropagateSignal((!this.bOverride) ? SignalType.Off : SignalType.On);
		this.ResolveSignalQueue();
		if (this.bOverride)
		{
			this.coSelf.ZeroCondAmount("IsOverrideOff");
		}
		else
		{
			this.coSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
		}
	}

	public bool GetOverride()
	{
		return this.bOverride;
	}

	public GateMode GateMode
	{
		get
		{
			return this.gateMode;
		}
		set
		{
			if (this.gateMode == value)
			{
				return;
			}
			this.gateMode = value;
			Dictionary<string, string> dictionary = this.mapGPM;
			string key = "gate";
			int num = (int)this.gateMode;
			dictionary[key] = num.ToString().ToLower();
		}
	}

	public void SetGateMode(int gate)
	{
		if (this.gateMode == (GateMode)gate)
		{
			return;
		}
		this.gateMode = (GateMode)gate;
		this.mapGPM["gate"] = gate.ToString().ToLower();
	}

	public float DelaySlider
	{
		get
		{
			if (this.actSliderCallback != null)
			{
				this.actSliderCallback(this.GetSliderReal());
			}
			return this.sliderPercent;
		}
		set
		{
			if (this.sliderPercent == value)
			{
				return;
			}
			if (this.actSliderCallback != null)
			{
				this.actSliderCallback(this.GetSliderReal());
			}
			this.sliderPercent = value;
			this.mapGPM["delay"] = this.sliderPercent.ToString();
		}
	}

	public float GetSliderReal()
	{
		return this.sliderMax * this.sliderPercent + 0.1f;
	}

	public void SetSlider(float slide)
	{
		if (this.sliderPercent == slide)
		{
			return;
		}
		float num = slide * this.sliderMax / 0.1f;
		num = (float)Mathf.RoundToInt(num);
		num *= 0.1f;
		this.sliderPercent = num / this.sliderMax;
		this.mapGPM["delay"] = this.sliderPercent.ToString();
		this.coSelf.mapGUIPropMaps[this.strGPMKey] = this.mapGPM;
		if (this.actSliderCallback != null)
		{
			this.actSliderCallback(num + 0.1f);
		}
	}

	public void SetGPM()
	{
		this.mapGPM["status"] = this.status.ToString().ToLower();
		if (this.coShip == null)
		{
			this.coShip = this.coSelf.ship;
		}
		string text = string.Empty;
		bool flag = false;
		foreach (ElectricalConnection electricalConnection in this.inputConnections.Values)
		{
			if (flag)
			{
				text += ",";
			}
			else
			{
				flag = true;
			}
			text += electricalConnection.ToString();
		}
		this.mapGPM["inputConnections"] = text;
		text = string.Empty;
		flag = false;
		foreach (ElectricalConnection electricalConnection2 in this.outputConnections.Values)
		{
			if (flag)
			{
				text += ",";
			}
			else
			{
				flag = true;
			}
			text += electricalConnection2.ToString();
		}
		this.mapGPM["outputConnections"] = text;
		text = string.Empty;
		flag = false;
		foreach (ElectricalSignal electricalSignal in this.signalQueue)
		{
			if (flag)
			{
				text += ",";
			}
			else
			{
				flag = true;
			}
			text += electricalSignal.ToString();
		}
		this.mapGPM["signalQueue"] = text;
		Dictionary<string, string> dictionary = this.mapGPM;
		string key = "gate";
		int num = (int)this.gateMode;
		dictionary[key] = num.ToString().ToLower();
		this.coSelf.mapGUIPropMaps[this.strGPMKey] = this.mapGPM;
	}

	public void AuditConnections()
	{
		int num = 0;
		List<string> list = new List<string>();
		foreach (ElectricalConnection electricalConnection in this.inputConnections.Values)
		{
			CondOwner cobyID = this.coShip.GetCOByID(electricalConnection.originID);
			if (cobyID == null)
			{
				list.Add(electricalConnection.originID);
			}
			else
			{
				Electrical electrical = cobyID.Electrical;
				if (electrical == null)
				{
					list.Add(electricalConnection.originID);
				}
			}
		}
		foreach (string key in list)
		{
			this.inputConnections.Remove(key);
			num++;
		}
		list.Clear();
		foreach (ElectricalConnection electricalConnection2 in this.outputConnections.Values)
		{
			CondOwner cobyID2 = this.coShip.GetCOByID(electricalConnection2.originID);
			if (cobyID2 == null)
			{
				list.Add(electricalConnection2.originID);
			}
			else
			{
				Electrical electrical2 = cobyID2.Electrical;
				if (electrical2 == null)
				{
					list.Add(electricalConnection2.originID);
				}
			}
		}
		foreach (string key2 in list)
		{
			this.outputConnections.Remove(key2);
			num++;
		}
		if (num > 0)
		{
			ConsoleToGUI.instance.LogInfo("Removed " + num.ToString() + " faulty connections!");
		}
		this.SetGPM();
		this.fTimeToNextAudit = MathUtils.Rand(500.0, 1500.0, MathUtils.RandType.Flat, null);
	}

	private void OnDestroy()
	{
		if (this.needsCleanup)
		{
			this.CleanUp(true);
		}
	}

	public void CleanUp(bool disconnect)
	{
		this.needsCleanup = false;
		if (!disconnect)
		{
			return;
		}
		if (this.coShip == null && this.coSelf != null)
		{
			this.coShip = this.coSelf.ship;
		}
		if (this.coShip == null)
		{
			return;
		}
		foreach (ElectricalConnection electricalConnection in this.inputConnections.Values)
		{
			CondOwner cobyID = this.coShip.GetCOByID(electricalConnection.originID);
			if (!(cobyID == null))
			{
				Electrical electrical = cobyID.Electrical;
				if (!(electrical == null))
				{
					electrical.outputConnections.Remove(this.coSelf.strID);
				}
			}
		}
		foreach (ElectricalConnection electricalConnection2 in this.outputConnections.Values)
		{
			CondOwner cobyID2 = this.coShip.GetCOByID(electricalConnection2.originID);
			if (!(cobyID2 == null))
			{
				Electrical electrical2 = cobyID2.Electrical;
				if (!(electrical2 == null))
				{
					electrical2.AddSignal(new ElectricalSignal(this.coSelf.strID, SignalType.Disconnect, false));
				}
			}
		}
	}

	public ElectricalConnection SetUpConnection(CondOwner co)
	{
		ElectricalConnection electricalConnection = new ElectricalConnection(co.strID, SignalType.None, string.Empty, true);
		if (this.outputConnections.ContainsKey(co.strID))
		{
			electricalConnection = this.outputConnections[co.strID];
		}
		else
		{
			this.outputConnections.Add(co.strID, electricalConnection);
			this.QueueSignal(co.strID, SignalType.Connect);
			this.QueueSignal(co.strID, SignalType.On);
		}
		this.SetGPM();
		return electricalConnection;
	}

	public void RemoveConnection(string strID)
	{
		this.QueueSignal(strID, SignalType.Disconnect);
		if (this.outputConnections.ContainsKey(strID))
		{
			this.outputConnections.Remove(strID);
		}
		this.SetGPM();
	}

	private const double processDelay = 0.1;

	private const double fAuditPeriod = 1000.0;

	public CondOwner coSelf;

	public Ship coShip;

	private string strGPMKey = "Electrical";

	public bool isInitialising = true;

	public int frameCount;

	public bool needsCleanup = true;

	public bool bOverride = true;

	public bool status = true;

	public double fTimeToNextAudit = 1.0;

	public GateMode gateMode;

	public float sliderPercent;

	private float sliderMax = 60f;

	public Dictionary<string, ElectricalConnection> inputConnections = new Dictionary<string, ElectricalConnection>();

	public Dictionary<string, ElectricalConnection> outputConnections = new Dictionary<string, ElectricalConnection>();

	private List<string> outputRemoves = new List<string>();

	private List<ElectricalSignal> signalQueue = new List<ElectricalSignal>();

	private List<ElectricalSignal> sendQueue = new List<ElectricalSignal>();

	private bool bNeedsCheck = true;

	private double fTimeOfNextSignalCheck;

	private Dictionary<string, string> mapGPM = new Dictionary<string, string>();

	private Action<float> actSliderCallback;
}
