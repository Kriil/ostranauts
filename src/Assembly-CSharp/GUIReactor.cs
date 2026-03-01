using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Reactor/inertial-confinement control panel. Likely mirrors one installed
// reactor CondOwner and writes UI toggle/knob state back into its prop map.
public class GUIReactor : GUIData
{
	// Unity setup: caches panel widgets, wires knob/toggle callbacks, and binds
	// button audio for the reactor console controls.
	protected override void Awake()
	{
		base.Awake();
		this.tf = base.transform;
		this.dictBatts = new Dictionary<string, Powered>();
		this.dictCOIDs = new Dictionary<string, string>();
		this.aInstLampsGreen = new List<GUILamp>();
		this.aInstLampsFound = new List<GUILamp>();
		this.aInstLampsOff = new List<GUILamp>();
		this.dictPnlLeds = new Dictionary<string, GUILedMeter>();
		this.GetUIRefs();
		this.knobBus.Callback = new Action<int>(this.SetPowerBus);
		this.knobPump.Callback = new Action<int>(this.SetPump);
		this.chkAlign.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkAlign", this.chkAlign.isOn.ToString());
		});
		this.chkCoilFwd.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkCoilFwd", this.chkCoilFwd.isOn.ToString());
		});
		this.chkCoilRear.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkCoilRear", this.chkCoilRear.isOn.ToString());
		});
		this.chkCryo.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkCryo", this.chkCryo.isOn.ToString());
		});
		this.chkFuelReg.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkFuelReg", this.chkFuelReg.isOn.ToString());
		});
		this.chkIgnition.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkIgnition", this.chkIgnition.isOn.ToString());
			if (!this.chkIgnition.isOn)
			{
				this.knobBus.State = 0;
			}
		});
		this.chkMHDOn.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkMHDOn", this.chkMHDOn.isOn.ToString());
		});
		this.chkPellet.onValueChanged.AddListener(delegate(bool A_1)
		{
			base.SetPropMapData("chkPellet", this.chkPellet.isOn.ToString());
		});
		this.slidCycle.onValueChanged.AddListener(delegate(float A_1)
		{
			if (!this.bPauseListenersReactor)
			{
				base.SetPropMapData("slidCycle", this.slidCycle.value.ToString());
			}
		});
		this.slidFlow.onValueChanged.AddListener(delegate(float A_1)
		{
			if (!this.bPauseListenersReactor)
			{
				base.SetPropMapData("slidFlow", this.slidFlow.value.ToString());
			}
		});
		this.chkThrustSafety.chkSwitch.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleThrustSafety();
		});
		AudioManager.AddBtnAudio(this.chkAlign.gameObject, "ShipUIBtnReactorAlignOn", "ShipUIBtnReactorAlignOff");
		AudioManager.AddBtnAudio(this.chkCoilFwd.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
		AudioManager.AddBtnAudio(this.chkCoilRear.gameObject, "ShipUIBtnReactorCoilRearIn", "ShipUIBtnReactorCoilRearOut");
		AudioManager.AddBtnAudio(this.chkCryo.gameObject, "ShipUIBtnReactorCryoOn", "ShipUIBtnReactorCryoOff");
		AudioManager.AddBtnAudio(this.chkFuelReg.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
		AudioManager.AddBtnAudio(this.chkIgnition.gameObject, "ShipUIBtnReactorIgnitionOn", "ShipUIBtnReactorIgnitionOff");
		AudioManager.AddBtnAudio(this.chkMHDOn.gameObject, "ShipUIBtnReactorMHDOn", "ShipUIBtnReactorMHDOff");
		AudioManager.AddBtnAudio(this.chkPellet.gameObject, "ShipUIBtnReactorPelletOn", "ShipUIBtnReactorPelletOff");
		AudioManager.AddBtnAudio(this.chkThrustSafety.chkSwitch.gameObject, "ShipUIBtnNSMinusIn", "ShipUIBtnNSMinusOut");
	}

	// Main panel refresh loop. Reads reactor Conditions each frame and enforces
	// UI interlocks such as ignition, bus state, readiness, and shutdown rules.
	private void Update()
	{
		double num = (double)CrewSim.TimeElapsedScaled();
		if (num == 0.0)
		{
			return;
		}
		this.fTimeInitLeft -= num;
		if (this.COSelf == null || this.COSelf.bDestroyed || !this.COSelf.HasCond("IsReactorIC"))
		{
			CrewSim.LowerUI(false);
			return;
		}
		if (this.knobBus.State == 0)
		{
			if (this.chkCoilFwd.isOn)
			{
				this.chkCoilFwd.isOn = false;
			}
			if (this.chkCoilRear.isOn)
			{
				this.chkCoilRear.isOn = false;
			}
			if (this.chkFuelReg.isOn)
			{
				this.chkFuelReg.isOn = false;
			}
			return;
		}
		if (this.COSelf.HasCond("IsOff"))
		{
			if (this.nWidgetState != 0)
			{
				this.SetWidgets(0);
			}
			else if (this.knobBus.State != 0)
			{
				if (this.nReadyCount == 0)
				{
					this.knobBus.State = 0;
				}
				else if (this.chkIgnition.isOn)
				{
					this.knobBus.State = 0;
				}
			}
			if (this.knobBus.State == 0)
			{
				return;
			}
		}
		else if (this.knobBus.State == 1 && this.nWidgetState == 0)
		{
			this.SetWidgets(1);
		}
		else if (this.knobBus.State == 2 && this.nWidgetState != 3)
		{
			this.SetWidgets(3);
		}
		if (this.nReadyCount > 0)
		{
			this.HandleInit();
			return;
		}
		double num2 = 1.0 - this.COSelf.GetCondAmount("StatICCapA");
		if (num2 > 0.004000000189989805)
		{
			if (this.nWidgetState > 0 && this.bmpCap.State != 2)
			{
				this.bmpCap.State = 2;
			}
		}
		else if (this.nWidgetState > 0 && this.bmpCap.State != 3)
		{
			this.bmpCap.State = 3;
		}
		if (this.chkPellet.isOn && this.nWidgetState > 0 && this.bmpFeed.State == 0)
		{
			this.bmpFeed.State = 3;
		}
		if (this.chkAlign.isOn && this.nWidgetState > 0 && this.bmpAlign.State == 0)
		{
			this.bmpAlign.State = 3;
		}
		this.UpdateUI();
	}

	private new void UpdateUI()
	{
		if (this.COSelf == null)
		{
			return;
		}
		FusionIC component = this.COSelf.GetComponent<FusionIC>();
		if (component == null)
		{
			return;
		}
		foreach (string key in this.dictBatts.Keys)
		{
			if (this.dictBatts[key] == null)
			{
				CondOwner co = this.GetCO(this.dictCOIDs[key]);
				if (co != null)
				{
					this.dictBatts[key] = co.Pwr;
				}
			}
			Powered powered = this.dictBatts[key];
			if (powered != null)
			{
				double condAmount = powered.CO.GetCondAmount("StatPower");
				double num = powered.PowerStoredMax;
				if (num == 0.0)
				{
					num = 1.0;
				}
				this.dictPnlLeds[key].SetValue(Convert.ToSingle(condAmount / num));
			}
		}
		this.dictPnlLeds["pnlLedsPwrTotal"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPwrTotal")) * 2f);
		this.dictPnlLeds["pnlLedsPwrFus"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPwrFus")) * 2f);
		this.dictPnlLeds["pnlLedsPwrMHD"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPwrMHD")) * 2f);
		this.dictPnlLeds["pnlLedsPwrThr"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPwrThrust")) * 2f);
		this.dictPnlLeds["pnlLedsPwrLoad"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPwrLoad")) * 2f);
		this.dictPnlLeds["pnlLedsCoreTemp"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICCoreTemp")));
		this.dictPnlLeds["pnlLedsPressureA"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPressureA")));
		this.dictPnlLeds["pnlLedsCapA"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICCapA")));
		this.dictPnlLeds["pnlLedsPressureB"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICPressureA")));
		this.dictPnlLeds["pnlLedsCapB"].SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICCapA")));
		int num2 = 0;
		for (int i = 0; i < this.aInstLampsGreen.Count; i++)
		{
			FusionIC.ModuleStatus moduleStatus = FusionIC.ModuleStatus.Missing;
			if (this.knobBus.State == 0)
			{
				this.SetInstructionLamp(moduleStatus, i);
			}
			else
			{
				if (i == 0)
				{
					if (this.knobBus.State > 0)
					{
						moduleStatus = FusionIC.ModuleStatus.On;
					}
					else
					{
						moduleStatus = FusionIC.ModuleStatus.Off;
					}
				}
				else if (i == 1)
				{
					if (this.COSelf.GetCondAmount("StatICPressureA") < 0.15000000596046448)
					{
						moduleStatus = FusionIC.ModuleStatus.On;
					}
					else
					{
						moduleStatus = FusionIC.ModuleStatus.Off;
					}
				}
				else if (i == 2)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.Capacitor);
					if (moduleStatus == FusionIC.ModuleStatus.On && 1.0 - this.COSelf.GetCondAmount("StatICCapA") >= 0.004000000189989805)
					{
						moduleStatus = FusionIC.ModuleStatus.Off;
					}
					if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.bmpCap.State = 0;
						this.bmpCapLbl.State = 0;
					}
				}
				else if (i == 3)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.Laser);
					if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.bmpAlignLbl.State = 0;
						this.bmpAlign.State = 0;
						this.chkAlign.isOn = false;
					}
					else
					{
						if (moduleStatus != FusionIC.ModuleStatus.On && this.bmpAlign.State == 3)
						{
							this.bmpAlign.State = 1;
						}
						if (this.bmpAlign.State == 3)
						{
							moduleStatus = FusionIC.ModuleStatus.On;
						}
						else
						{
							moduleStatus = FusionIC.ModuleStatus.Off;
						}
					}
				}
				else if (i == 4)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.PelletFeed);
					if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.chkPellet.isOn = false;
						this.bmpFeed.State = 0;
						this.bmpFeedLbl.State = 0;
					}
				}
				else if (i == 5)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.CryoPump);
					if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.chkCryo.isOn = false;
					}
				}
				else if (i == 6)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.FuelReg);
					if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.chkFuelReg.isOn = false;
					}
				}
				else if (i == 7)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.Coil);
					if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.chkCoilFwd.isOn = false;
						this.chkCoilRear.isOn = false;
					}
				}
				else if (i == 8)
				{
					moduleStatus = component.GetModuleStatus(FusionIC.Module.MHD);
					if (moduleStatus == FusionIC.ModuleStatus.On && !this.chkMHDOn.isOn)
					{
						moduleStatus = FusionIC.ModuleStatus.Off;
					}
					else if (moduleStatus == FusionIC.ModuleStatus.Missing)
					{
						this.chkMHDOn.isOn = false;
					}
				}
				else if (i == 9)
				{
					if (this.chkIgnition.isOn)
					{
						moduleStatus = FusionIC.ModuleStatus.On;
					}
					else
					{
						moduleStatus = FusionIC.ModuleStatus.Off;
					}
				}
				this.SetInstructionLamp(moduleStatus, i);
				if (i != 5 && i < 7 && moduleStatus == FusionIC.ModuleStatus.On)
				{
					num2++;
				}
			}
		}
		bool flag = this.COSelf.HasCond("IsReadyFusion");
		if (flag || num2 == this.aInstLampsGreen.Count - 4)
		{
			this.bmpIgnitReady.State = 3;
		}
		this.segD.SetValue(Convert.ToSingle(component.ReactantD));
		this.segDRate.SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICDRate") * 87658.125));
		this.segHe3.SetValue(Convert.ToSingle(component.ReactantHe3));
		this.segHe3Rate.SetValue(Convert.ToSingle(this.COSelf.GetCondAmount("StatICHe3Rate") * 87658.125));
		this.bmpXRay.State = 0;
		if (this.COSelf.HasCond("DcABLWall05"))
		{
			this.bmpWall.State = 3;
			if (flag)
			{
				this.bmpXRay.State = 3;
			}
		}
		else if (this.COSelf.HasCond("DcABLWall04"))
		{
			this.bmpWall.State = 3;
		}
		else if (this.COSelf.HasCond("DcABLWall03"))
		{
			this.bmpWall.State = 2;
		}
		else
		{
			this.bmpWall.State = 0;
		}
		bool flag2 = false;
		bool.TryParse(base.GetPropMapData("bNWZ"), out flag2);
		if (flag2)
		{
			if (StarSystem.fEpoch - this.fEpochStationBegin < 2.0)
			{
				this.bmpThrustWarn.State = 2;
			}
			else
			{
				this.bmpThrustWarn.State = 3;
			}
		}
		else
		{
			this.bmpThrustWarn.State = 0;
		}
		if (this.chkThrustSafety.chkSwitch.isOn && this.bmpThrustWarn.State != 0)
		{
			this.chkThrustSafety.chkSwitch.isOn = false;
		}
		if (StarSystem.fEpoch - this.fEpochThrustSafetyBegin < 2.0)
		{
			this.bmpThrustSafety.State = 2;
		}
		else if (this.chkThrustSafety.chkSwitch.isOn)
		{
			this.bmpThrustSafety.State = 3;
		}
		else
		{
			this.bmpThrustSafety.State = 0;
		}
		if (!this.gplCycle.bPressed)
		{
			this.bPauseListenersReactor = true;
			float num3 = 0f;
			if (float.TryParse(this.COSelf.ship.GetReactorGPMValue("slidCycle"), out num3))
			{
				if (num3 == 0f && this.slidCycle.value != 0f)
				{
					if (flag2)
					{
						this.FlashStationWarn();
					}
					if (!this.chkThrustSafety.chkSwitch.isOn)
					{
						this.FlashThrustSafety();
					}
				}
				this.slidCycle.value = num3;
			}
			this.bPauseListenersReactor = false;
		}
		if (!this.gplFlow.bPressed)
		{
			this.bPauseListenersReactor = true;
			float value = 0f;
			if (float.TryParse(this.COSelf.ship.GetReactorGPMValue("slidFlow"), out value))
			{
				this.slidFlow.value = value;
			}
			this.bPauseListenersReactor = false;
		}
	}

	private void SetInstructionLamp(FusionIC.ModuleStatus mstat, int i)
	{
		if (mstat != FusionIC.ModuleStatus.Missing)
		{
			if (mstat != FusionIC.ModuleStatus.Off)
			{
				if (mstat == FusionIC.ModuleStatus.On)
				{
					this.aInstLampsOff[i].State = 0;
					this.aInstLampsGreen[i].State = 3;
					this.aInstLampsFound[i].State = 3;
				}
			}
			else
			{
				this.aInstLampsOff[i].State = 3;
				this.aInstLampsGreen[i].State = 0;
				this.aInstLampsFound[i].State = 3;
			}
		}
		else
		{
			this.aInstLampsOff[i].State = 0;
			this.aInstLampsGreen[i].State = 0;
			this.aInstLampsFound[i].State = 0;
		}
	}

	private void SetPump(int nState)
	{
		if (nState != 1)
		{
			if (nState != 2)
			{
			}
		}
		base.SetPropMapData("knobPump", this.knobPump.State.ToString());
	}

	private void SetPowerBus(int nState)
	{
		this.COSelf.AddCondAmount("IsOverrideOff", -this.COSelf.GetCondAmount("IsOverrideOff"), 0.0, 0f);
		this.COSelf.AddCondAmount("IsOverrideOn", -this.COSelf.GetCondAmount("IsOverrideOn"), 0.0, 0f);
		FusionIC component = this.COSelf.GetComponent<FusionIC>();
		if (nState != 1)
		{
			if (nState != 2)
			{
				this.SetWidgets(nState);
				this.COSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
				this.knobPump.State = 0;
				this.chkCryo.isOn = false;
				this.chkMHDOn.isOn = false;
				this.chkIgnition.isOn = false;
				this.chkPellet.isOn = false;
				this.chkAlign.isOn = false;
				this.chkCoilFwd.isOn = false;
				this.chkCoilRear.isOn = false;
				this.chkFuelReg.isOn = false;
				this.slidFlow.value = this.slidFlow.minValue;
				this.slidCycle.value = this.slidCycle.minValue;
				this.chkThrustSafety.chkSwitch.isOn = false;
			}
			else
			{
				this.SetWidgets(3);
				if (component != null && component.MHDOn)
				{
					this.COSelf.AddCondAmount("IsReadyRecharge", 1.0, 0.0, 0f);
				}
				this.CheckBatts();
			}
		}
		else
		{
			if (!this.bSkipInit && this.nWidgetState == 0)
			{
				this.SetWidgets(nState);
				this.nReadyCount = 10;
				this.fTimeInitLeft = 10.0;
				if (component != null)
				{
					component.GetReactants();
				}
			}
			else
			{
				this.SetWidgets(3);
			}
			this.COSelf.ZeroCondAmount("IsReadyRecharge");
			this.CheckBatts();
		}
		base.SetPropMapData("knobBus", this.knobBus.State.ToString());
	}

	private void SetWidgets(int nState)
	{
		if (nState != 1)
		{
			if (nState != 3)
			{
				foreach (KeyValuePair<string, GUILedMeter> keyValuePair in this.dictPnlLeds)
				{
					keyValuePair.Value.SetState(0);
				}
				foreach (GUILamp guilamp in this.aInstLampsGreen)
				{
					guilamp.State = 0;
				}
				foreach (GUILamp guilamp2 in this.aInstLampsFound)
				{
					guilamp2.State = 0;
				}
				foreach (GUILamp guilamp3 in this.aInstLampsOff)
				{
					guilamp3.State = 0;
				}
				this.bmpXRay.State = 0;
				this.bmpWall.State = 0;
				this.bmpCap.State = 0;
				this.bmpAlign.State = 0;
				this.bmpFeed.State = 0;
				this.bmpXRayLbl.State = 0;
				this.bmpWallLbl.State = 0;
				this.bmpCapLbl.State = 0;
				this.bmpAlignLbl.State = 0;
				this.bmpFeedLbl.State = 0;
				this.bmpIgnitReady.State = 0;
				this.bmpThrustWarn.State = 0;
				Toggle toggle = this.chkFuelReg;
				bool flag = false;
				this.chkCoilRear.isOn = flag;
				flag = flag;
				this.chkCoilFwd.isOn = flag;
				toggle.isOn = flag;
				this.segD.State = 0;
				this.segDRate.State = 0;
				this.segHe3.State = 0;
				this.segHe3Rate.State = 0;
			}
			else
			{
				foreach (KeyValuePair<string, GUILedMeter> keyValuePair2 in this.dictPnlLeds)
				{
					keyValuePair2.Value.SetState(2);
				}
				foreach (GUILamp guilamp4 in this.aInstLampsGreen)
				{
					guilamp4.State = 0;
				}
				foreach (GUILamp guilamp5 in this.aInstLampsFound)
				{
					guilamp5.State = 2;
				}
				foreach (GUILamp guilamp6 in this.aInstLampsOff)
				{
					guilamp6.State = 0;
				}
				this.bmpXRay.State = 0;
				this.bmpWall.State = 0;
				this.bmpCap.State = 0;
				this.bmpAlign.State = 0;
				this.bmpFeed.State = 0;
				this.bmpXRayLbl.State = 3;
				this.bmpWallLbl.State = 3;
				this.bmpCapLbl.State = 3;
				this.bmpAlignLbl.State = 3;
				this.bmpFeedLbl.State = 3;
				this.bmpIgnitReady.State = 0;
				this.bmpThrustWarn.State = 0;
				this.segD.State = 2;
				this.segDRate.State = 2;
				this.segHe3.State = 2;
				this.segHe3Rate.State = 2;
			}
		}
		else
		{
			foreach (KeyValuePair<string, GUILedMeter> keyValuePair3 in this.dictPnlLeds)
			{
				if (keyValuePair3.Value.State == 0)
				{
					keyValuePair3.Value.SetState(1);
				}
			}
			foreach (GUILamp guilamp7 in this.aInstLampsGreen)
			{
				if (guilamp7.State == 0)
				{
					guilamp7.State = 1;
				}
			}
			foreach (GUILamp guilamp8 in this.aInstLampsFound)
			{
				if (guilamp8.State == 0)
				{
					guilamp8.State = 1;
				}
			}
			foreach (GUILamp guilamp9 in this.aInstLampsOff)
			{
				if (guilamp9.State == 0)
				{
					guilamp9.State = 1;
				}
			}
			if (this.bmpXRay.State == 0)
			{
				this.bmpXRay.State = 1;
			}
			if (this.bmpWall.State == 0)
			{
				this.bmpWall.State = 1;
			}
			if (this.bmpCap.State == 0)
			{
				this.bmpCap.State = 1;
			}
			if (this.bmpAlign.State == 0)
			{
				this.bmpAlign.State = 1;
			}
			if (this.bmpFeed.State == 0)
			{
				this.bmpFeed.State = 1;
			}
			if (this.bmpXRayLbl.State == 0)
			{
				this.bmpXRayLbl.State = 1;
			}
			if (this.bmpWallLbl.State == 0)
			{
				this.bmpWallLbl.State = 1;
			}
			if (this.bmpCapLbl.State == 0)
			{
				this.bmpCapLbl.State = 1;
			}
			if (this.bmpAlignLbl.State == 0)
			{
				this.bmpAlignLbl.State = 1;
			}
			if (this.bmpFeedLbl.State == 0)
			{
				this.bmpFeedLbl.State = 1;
			}
			if (this.bmpThrustWarn.State == 0)
			{
				this.bmpThrustWarn.State = 1;
			}
			if (this.bmpIgnitReady.State == 0)
			{
				this.bmpIgnitReady.State = 1;
			}
			if (this.segD.State == 0)
			{
				this.segD.State = 1;
			}
			if (this.segDRate.State == 0)
			{
				this.segDRate.State = 1;
			}
			if (this.segHe3.State == 0)
			{
				this.segHe3.State = 1;
			}
			if (this.segHe3Rate.State == 0)
			{
				this.segHe3Rate.State = 1;
			}
		}
		this.nWidgetState = nState;
	}

	private void ToggleThrustSafety()
	{
		bool flag = this.chkThrustSafety.chkSwitch.isOn;
		if (flag && this.bmpThrustWarn.State != 0)
		{
			flag = false;
			this.FlashStationWarn();
		}
		this.chkThrustSafety.chkSwitch.isOn = flag;
		if (flag)
		{
			base.SetPropMapData("knobRatio", "1");
		}
		else
		{
			base.SetPropMapData("knobRatio", "0");
		}
	}

	private void FlashStationWarn()
	{
		this.fEpochStationBegin = StarSystem.fEpoch;
		AudioManager.am.PlayAudioEmitter("ShipUIReactorError", false, true);
	}

	private void FlashThrustSafety()
	{
		this.fEpochThrustSafetyBegin = StarSystem.fEpoch;
		AudioManager.am.PlayAudioEmitter("ShipUIReactorError", false, true);
	}

	private void HandleInit()
	{
		if (this.bmpXRayLbl.State == 2 && (this.fTimeInitLeft <= 0.0 || UnityEngine.Random.Range(0f, 1f) < 0.0125f))
		{
			this.nReadyCount--;
			this.bmpXRayLbl.State = 3;
		}
		if (this.bmpWallLbl.State == 2 && (this.fTimeInitLeft <= 0.0 || UnityEngine.Random.Range(0f, 1f) < 0.0125f))
		{
			this.nReadyCount--;
			this.bmpWallLbl.State = 3;
		}
		if (this.bmpCapLbl.State == 2 && UnityEngine.Random.Range(0f, 1f) < 0.0125f)
		{
			this.nReadyCount--;
			this.bmpCapLbl.State = 3;
		}
		if (this.bmpAlignLbl.State == 2 && UnityEngine.Random.Range(0f, 1f) < 0.0125f)
		{
			this.nReadyCount--;
			this.bmpAlignLbl.State = 3;
		}
		if (this.bmpFeedLbl.State == 2 && UnityEngine.Random.Range(0f, 1f) < 0.0125f)
		{
			this.nReadyCount--;
			this.bmpFeedLbl.State = 3;
		}
		if (this.bmpXRay.State == 2)
		{
			this.nReadyCount--;
			this.bmpXRay.State = 0;
		}
		if (this.bmpWall.State == 2)
		{
			this.nReadyCount--;
			this.bmpWall.State = 0;
		}
		if (this.bmpCap.State == 2)
		{
			this.nReadyCount--;
			this.bmpCap.State = 0;
		}
		if (this.bmpAlign.State == 2)
		{
			this.nReadyCount--;
			this.bmpAlign.State = 0;
		}
		if (this.bmpFeed.State == 2)
		{
			this.nReadyCount--;
			this.bmpFeed.State = 0;
		}
		if (this.nReadyCount == 0)
		{
			foreach (GUILamp guilamp in this.aInstLampsGreen)
			{
				guilamp.State = 0;
			}
			foreach (GUILamp guilamp2 in this.aInstLampsFound)
			{
				guilamp2.State = 2;
			}
			foreach (GUILamp guilamp3 in this.aInstLampsOff)
			{
				guilamp3.State = 0;
			}
			if (this.bmpIgnitReady.State == 2)
			{
				this.bmpIgnitReady.State = 0;
			}
			if (this.bmpThrustWarn.State == 2)
			{
				this.bmpThrustWarn.State = 0;
			}
			if (this.bmpThrustSafety.State == 2)
			{
				this.bmpThrustSafety.State = 0;
			}
		}
	}

	private void GetUIRefs()
	{
		this.dictPnlLeds["pnlLedsBatt01L"] = this.tf.Find("pnlBatt01/pnlLedsL").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsBatt01R"] = this.tf.Find("pnlBatt01/pnlLedsR").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsBatt02L"] = this.tf.Find("pnlBatt02/pnlLedsL").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsBatt02R"] = this.tf.Find("pnlBatt02/pnlLedsR").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPwrTotal"] = this.tf.Find("pnlPower/pnlLedsTotal").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPwrFus"] = this.tf.Find("pnlPower/pnlLedsFus").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPwrMHD"] = this.tf.Find("pnlPower/pnlLedsMHD").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPwrThr"] = this.tf.Find("pnlPower/pnlLedsThr").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPwrLoad"] = this.tf.Find("pnlPower/pnlLedsLoad").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsCoreTemp"] = this.tf.Find("pnlCoreTemp/pnlLeds").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPressureA"] = this.tf.Find("pnlCorePressCap/pnlLedsPressureA").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsPressureB"] = this.tf.Find("pnlCorePressCap/pnlLedsPressureB").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsCapA"] = this.tf.Find("pnlCorePressCap/pnlLedsCapA").GetComponent<GUILedMeter>();
		this.dictPnlLeds["pnlLedsCapB"] = this.tf.Find("pnlCorePressCap/pnlLedsCapB").GetComponent<GUILedMeter>();
		this.knobBus = this.tf.Find("pnlPower/knobBus").GetComponent<GUIKnob>();
		this.knobPump = this.tf.Find("pnlCoilPump/knobPump").GetComponent<GUIKnob>();
		this.bmpXRay = this.tf.Find("pnlLamps/bmpXRay").GetComponent<GUILamp>();
		this.bmpWall = this.tf.Find("pnlLamps/bmpWall").GetComponent<GUILamp>();
		this.bmpCap = this.tf.Find("pnlLamps/bmpCap").GetComponent<GUILamp>();
		this.bmpAlign = this.tf.Find("pnlLamps/bmpAlign").GetComponent<GUILamp>();
		this.bmpFeed = this.tf.Find("pnlLamps/bmpFeed").GetComponent<GUILamp>();
		this.bmpXRayLbl = this.tf.Find("pnlLamps/bmpXRayLbl").GetComponent<GUILamp>();
		this.bmpWallLbl = this.tf.Find("pnlLamps/bmpWallLbl").GetComponent<GUILamp>();
		this.bmpCapLbl = this.tf.Find("pnlLamps/bmpCapLbl").GetComponent<GUILamp>();
		this.bmpAlignLbl = this.tf.Find("pnlLamps/bmpAlignLbl").GetComponent<GUILamp>();
		this.bmpFeedLbl = this.tf.Find("pnlLamps/bmpFeedLbl").GetComponent<GUILamp>();
		this.bmpIgnitReady = this.tf.Find("pnlIgnit/bmpIgnitReady").GetComponent<GUILamp>();
		this.bmpThrustSafety = this.tf.Find("pnlPower/bmpThrustSafety").GetComponent<GUILamp>();
		this.bmpThrustWarn = this.tf.Find("pnlPower/bmpThrustWarn").GetComponent<GUILamp>();
		this.segD = this.tf.Find("pnlFuel/pnlD").GetComponent<GUI7Seg>();
		this.segDRate = this.tf.Find("pnlFuel/pnlDRate").GetComponent<GUI7Seg>();
		this.segHe3 = this.tf.Find("pnlFuel/pnlHe3").GetComponent<GUI7Seg>();
		this.segHe3Rate = this.tf.Find("pnlFuel/pnlHe3Rate").GetComponent<GUI7Seg>();
		this.chkCryo = this.tf.Find("pnlCoreTemp/chkCryo").GetComponent<Toggle>();
		this.chkMHDOn = this.tf.Find("pnlPower/chkMHDOn").GetComponent<Toggle>();
		this.chkIgnition = this.tf.Find("pnlIgnit/chkIgnition").GetComponent<Toggle>();
		this.chkPellet = this.tf.Find("pnlLamps/chkPellet").GetComponent<Toggle>();
		this.chkAlign = this.tf.Find("pnlLamps/chkAlign").GetComponent<Toggle>();
		this.chkCoilFwd = this.tf.Find("pnlCoilPump/chkCoilFwd").GetComponent<Toggle>();
		this.chkCoilRear = this.tf.Find("pnlCoilPump/chkCoilRear").GetComponent<Toggle>();
		this.chkFuelReg = this.tf.Find("pnlCoilPump/chkFuelReg").GetComponent<Toggle>();
		this.slidCycle = this.tf.Find("pnlPower/SliderCycle").GetComponent<Slider>();
		this.slidFlow = this.tf.Find("pnlPower/SliderFlow").GetComponent<Slider>();
		this.gplCycle = this.slidCycle.gameObject.AddComponent<GUIPointerListener>();
		this.gplFlow = this.slidFlow.gameObject.AddComponent<GUIPointerListener>();
		this.chkThrustSafety = base.transform.Find("pnlPower/chkThrustSafety").GetComponent<GUISafetyToggle>();
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepBus/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepBus/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepBus/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepPurge/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepPurge/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepPurge/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepCap/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepCap/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepCap/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepAlign/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepAlign/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepAlign/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepPellet/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepPellet/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepPellet/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepCryo/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepCryo/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepCryo/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepFuel/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepFuel/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepFuel/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepCoil/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepCoil/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepCoil/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepMHD/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepMHD/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepMHD/bmpOff").GetComponent<GUILamp>());
		this.aInstLampsGreen.Add(this.tf.Find("pnlInit/pnlStepIgnit/bmpGreen").GetComponent<GUILamp>());
		this.aInstLampsFound.Add(this.tf.Find("pnlInit/pnlStepIgnit/bmpFound").GetComponent<GUILamp>());
		this.aInstLampsOff.Add(this.tf.Find("pnlInit/pnlStepIgnit/bmpOff").GetComponent<GUILamp>());
	}

	private void LoadCOStats()
	{
		if (this.COSelf == null)
		{
			Debug.LogError("GUIReactor Cannot load null CO: " + this.strCoSelfID);
			return;
		}
		if (this.GetPropMap() != null)
		{
			if (this.GetPropMap().ContainsKey("knobBus"))
			{
				this.bSkipInit = true;
				this.knobBus.State = (int)Convert.ToInt16(this.GetPropMap()["knobBus"]);
				this.bSkipInit = false;
			}
			if (this.GetPropMap().ContainsKey("knobPump"))
			{
				this.knobPump.State = (int)Convert.ToInt16(this.GetPropMap()["knobPump"]);
			}
			if (this.GetPropMap().ContainsKey("chkCryo"))
			{
				this.chkCryo.isOn = Convert.ToBoolean(this.GetPropMap()["chkCryo"]);
			}
			if (this.GetPropMap().ContainsKey("chkMHDOn"))
			{
				this.chkMHDOn.isOn = Convert.ToBoolean(this.GetPropMap()["chkMHDOn"]);
			}
			if (this.GetPropMap().ContainsKey("chkIgnition"))
			{
				this.chkIgnition.isOn = Convert.ToBoolean(this.GetPropMap()["chkIgnition"]);
			}
			if (this.GetPropMap().ContainsKey("chkPellet"))
			{
				this.chkPellet.isOn = Convert.ToBoolean(this.GetPropMap()["chkPellet"]);
			}
			if (this.GetPropMap().ContainsKey("chkAlign"))
			{
				this.chkAlign.isOn = Convert.ToBoolean(this.GetPropMap()["chkAlign"]);
			}
			if (this.GetPropMap().ContainsKey("chkCoilFwd"))
			{
				this.chkCoilFwd.isOn = Convert.ToBoolean(this.GetPropMap()["chkCoilFwd"]);
			}
			if (this.GetPropMap().ContainsKey("chkCoilRear"))
			{
				this.chkCoilRear.isOn = Convert.ToBoolean(this.GetPropMap()["chkCoilRear"]);
			}
			if (this.GetPropMap().ContainsKey("chkFuelReg"))
			{
				this.chkFuelReg.isOn = Convert.ToBoolean(this.GetPropMap()["chkFuelReg"]);
			}
			if (this.GetPropMap().ContainsKey("slidCycle"))
			{
				this.slidCycle.value = Convert.ToSingle(this.GetPropMap()["slidCycle"]);
			}
			if (this.GetPropMap().ContainsKey("slidFlow"))
			{
				this.slidFlow.value = Convert.ToSingle(this.GetPropMap()["slidFlow"]);
			}
			if (this.GetPropMap().ContainsKey("knobRatio"))
			{
				float num = Convert.ToSingle(this.GetPropMap()["knobRatio"]);
				this.chkThrustSafety.chkSwitch.isOn = (num > 0f);
			}
			if (this.dictPropMap.ContainsKey("bThrustSafetyCovered"))
			{
				this.chkThrustSafety.bSilent = true;
				this.chkThrustSafety.Closed = Convert.ToBoolean(this.GetPropMap()["bThrustSafetyCovered"]);
				this.chkThrustSafety.bSilent = false;
			}
		}
		if (this.knobBus.State != 0)
		{
			if (this.COSelf.HasCond("IsReadyFusion"))
			{
				this.chkIgnition.isOn = true;
			}
			this.CheckBatts();
		}
	}

	private void CheckBatts()
	{
		Powered pwr = this.COSelf.Pwr;
		if (pwr == null)
		{
			return;
		}
		string[] array = new string[]
		{
			"pnlLedsBatt01L",
			"pnlLedsBatt01R",
			"pnlLedsBatt02L",
			"pnlLedsBatt02R"
		};
		int num = 0;
		for (int i = 0; i < pwr.jsonPI.aInputPts.Length; i++)
		{
			if (num >= array.Length)
			{
				break;
			}
			Vector2 pos = this.COSelf.GetPos(pwr.jsonPI.aInputPts[i], false);
			Tile tileAtWorldCoords = this.COSelf.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
			if (!(tileAtWorldCoords == null))
			{
				foreach (Powered powered in tileAtWorldCoords.aConnectedPowerCOs)
				{
					if (!(powered == pwr))
					{
						if (num >= array.Length)
						{
							break;
						}
						if (!(powered == null) && !(powered.CO == null))
						{
							this.dictCOIDs[array[num]] = powered.CO.strID;
							this.dictBatts[array[num]] = powered;
							num++;
						}
					}
				}
			}
		}
	}

	private CondOwner GetCO(string strKey)
	{
		if (strKey == null)
		{
			return null;
		}
		CondOwner result = null;
		DataHandler.mapCOs.TryGetValue(strKey, out result);
		return result;
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SetPropMapData("bThrustSafetyCovered", this.chkThrustSafety.Closed.ToString().ToLower());
		base.SaveAndClose();
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.LoadCOStats();
	}

	public override CondOwner COSelf
	{
		set
		{
			base.COSelf = value;
		}
	}

	private Transform tf;

	private Dictionary<string, GUILedMeter> dictPnlLeds;

	private Dictionary<string, Powered> dictBatts;

	private List<GUILamp> aInstLampsFound;

	private List<GUILamp> aInstLampsGreen;

	private List<GUILamp> aInstLampsOff;

	private Dictionary<string, string> dictCOIDs;

	private GUILamp bmpXRayLbl;

	private GUILamp bmpWallLbl;

	private GUILamp bmpCapLbl;

	private GUILamp bmpAlignLbl;

	private GUILamp bmpFeedLbl;

	private GUILamp bmpXRay;

	private GUILamp bmpWall;

	private GUILamp bmpCap;

	private GUILamp bmpAlign;

	private GUILamp bmpFeed;

	private GUILamp bmpIgnitReady;

	private GUILamp bmpThrustWarn;

	private GUILamp bmpThrustSafety;

	private GUISafetyToggle chkThrustSafety;

	private GUI7Seg segHe3;

	private GUI7Seg segHe3Rate;

	private GUI7Seg segD;

	private GUI7Seg segDRate;

	private GUIKnob knobBus;

	private GUIKnob knobPump;

	private Toggle chkCryo;

	private Toggle chkMHDOn;

	private Toggle chkIgnition;

	private Toggle chkPellet;

	private Toggle chkAlign;

	private Toggle chkCoilFwd;

	private Toggle chkCoilRear;

	private Toggle chkFuelReg;

	private Slider slidFlow;

	private Slider slidCycle;

	private GUIPointerListener gplFlow;

	private GUIPointerListener gplCycle;

	private double fTimeInitLeft;

	private int nReadyCount;

	private int nWidgetState;

	private double fEpochStationBegin;

	private double fEpochThrustSafetyBegin;

	private bool bSkipInit;

	private bool bPauseListenersReactor;
}
