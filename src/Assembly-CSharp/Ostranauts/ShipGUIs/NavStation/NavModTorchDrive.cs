using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModTorchDrive : NavModBase
	{
		protected override void Awake()
		{
			base.Awake();
			this.btnReactorShutdown.onClick.AddListener(new UnityAction(this.ReactorShutDown));
			this.chkCycleSafety.chkSwitch.onValueChanged.AddListener(delegate(bool A_1)
			{
				this.ToggleCycleSafety();
			});
			this.chkTorchSafety.chkSwitch.onValueChanged.AddListener(delegate(bool _)
			{
				this.ToggleTorchSafety();
				this._guiOrbitDraw.SetPropMapData("bTorchSafetyCovered", this.chkTorchSafety.Closed.ToString().ToLower());
			});
			AudioManager.AddBtnAudio(this.chkTorchSafety.chkSwitch.gameObject, "ShipUIBtnNSMinusIn", "ShipUIBtnNSMinusOut");
			AudioManager.AddBtnAudio(this.chkCycleSafety.chkSwitch.gameObject, "ShipUIBtnNSMinusIn", "ShipUIBtnNSMinusOut");
			this.slidFlow.onValueChanged.AddListener(delegate(float A_1)
			{
				if (!this.bPauseListenersReactor)
				{
					this.COSelf.ship.SetReactorGPMValue("slidFlow", this.slidFlow.value.ToString());
					this.COSelf.ship.SetReactorGPMValue("fFlowEpochResume", (StarSystem.fEpoch + 2.0).ToString());
				}
			});
			this.slidCycle.onValueChanged.AddListener(delegate(float A_1)
			{
				if (!this.bPauseListenersReactor)
				{
					this._guiOrbitDraw.SetPropMapData("chkEngage", false.ToString());
					this.COSelf.ship.SetReactorGPMValue("slidCycle", this.slidCycle.value.ToString());
				}
			});
		}

		protected override void Init()
		{
			string reactorGPMValue;
			bool isOn = !this.dictPropMap.TryGetValue("bTorchSafety", out reactorGPMValue) || bool.Parse(reactorGPMValue);
			this.chkTorchSafety.chkSwitch.isOn = isOn;
			this.ToggleTorchSafety();
			if (this.dictPropMap.TryGetValue("bTorchSafetyCovered", out reactorGPMValue))
			{
				this.chkTorchSafety.bSilent = true;
				this.chkTorchSafety.Closed = bool.Parse(reactorGPMValue);
				this.chkTorchSafety.bSilent = false;
			}
			reactorGPMValue = this.COSelf.ship.GetReactorGPMValue("knobRatio");
			if (!string.IsNullOrEmpty(reactorGPMValue))
			{
				int num = int.Parse(reactorGPMValue);
				if (num == 1)
				{
					this.chkCycleSafety.chkSwitch.isOn = true;
				}
				else
				{
					this.chkCycleSafety.chkSwitch.isOn = false;
				}
				this.ToggleCycleSafety();
			}
			if (this.dictPropMap.TryGetValue("bCycleSafetyCovered", out reactorGPMValue))
			{
				this.chkCycleSafety.bSilent = true;
				this.chkCycleSafety.Closed = bool.Parse(reactorGPMValue);
				this.chkCycleSafety.bSilent = false;
			}
		}

		protected override void OnNavModMessage(NavModMessageType messageType, object arg)
		{
			if (messageType == NavModMessageType.UpdateUI)
			{
				this.CheckLamps();
				this.UpdateUI();
			}
		}

		private new void UpdateUI()
		{
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
			bool flag = false;
			bool.TryParse(this.COSelf.ship.GetReactorGPMValue("bNWZ"), out flag);
			if (!this.gplCycle.bPressed)
			{
				this.bPauseListenersReactor = true;
				float num = 0f;
				if (float.TryParse(this.COSelf.ship.GetReactorGPMValue("slidCycle"), out num))
				{
					if (num == 0f && this.slidCycle.value != 0f)
					{
						if (flag)
						{
							this._guiOrbitDraw.FlashStationWarn();
						}
						if (!this.chkCycleSafety.chkSwitch.isOn)
						{
							this._guiOrbitDraw.FlashCycleSafety();
						}
					}
					this.slidCycle.value = num;
				}
				this.bPauseListenersReactor = false;
			}
			if (this.glmCoreTemp.State != 2)
			{
				this.glmCoreTemp.SetState(2);
			}
			if (this.COSelf.ship.Reactor != null)
			{
				this.glmCoreTemp.SetValue(Convert.ToSingle(this.COSelf.ship.Reactor.GetCondAmount("StatICCoreTemp")));
			}
			else
			{
				this.glmCoreTemp.SetValue(0);
			}
			string reactorGPMValue = this.COSelf.ship.GetReactorGPMValue("knobRatio");
			if (!string.IsNullOrEmpty(reactorGPMValue))
			{
				int num2 = int.Parse(reactorGPMValue);
				if (num2 == 1)
				{
					this.chkCycleSafety.chkSwitch.isOn = true;
				}
				else
				{
					if (this.chkCycleSafety.chkSwitch.isOn && flag)
					{
						this._guiOrbitDraw.FlashStationWarn();
					}
					this.chkCycleSafety.chkSwitch.isOn = false;
				}
			}
			this.txtReactantLeft.text = (this.COSelf.ship.fShallowFusionRemain / 3600.0).ToString("#.00") + "h";
			this.txtAccel.text = this.COSelf.ship.Gravity.ToString("#.00") + "G";
		}

		private void ReactorShutDown()
		{
			if (this.COSelf.ship.Reactor != null)
			{
				this.COSelf.ship.Reactor.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
			}
		}

		private void CheckLamps()
		{
			if (this.chkTorchSafety.chkSwitch.isOn)
			{
				this.goTorchSafety.State = 3;
			}
			else
			{
				this.goTorchSafety.State = 0;
			}
			if (StarSystem.fEpoch - this._guiOrbitDraw.fEpochCycleSafetyBegin < 2.0)
			{
				this.goCycleSafety.State = 2;
			}
			else if (this.chkCycleSafety.chkSwitch.isOn)
			{
				this.goCycleSafety.State = 3;
			}
			else
			{
				this.goCycleSafety.State = 0;
			}
			bool flag = false;
			if (StarSystem.fEpoch - this._guiOrbitDraw.fEpochStationBegin < 2.0)
			{
				this.goNWZProx.State = 2;
			}
			else if (bool.TryParse(this.COSelf.ship.GetReactorGPMValue("bNWZ"), out flag) && flag)
			{
				this.goNWZProx.State = 3;
			}
			else
			{
				this.goNWZProx.State = 0;
			}
		}

		private float GetLimiterSafetyMax(Ship shipUs)
		{
			float num = 1f;
			double num2 = (double)(shipUs.GetMaxTorchThrust(num) / 6.684587E-12f) / 9.81;
			while (num2 > 2.0 && num > 0f)
			{
				num -= 0.01f;
				if (num < 0f)
				{
					num = 0f;
				}
				num2 = (double)(shipUs.GetMaxTorchThrust(num) / 6.684587E-12f) / 9.81;
			}
			return num;
		}

		private void ToggleCycleSafety()
		{
			if (this.chkCycleSafety.chkSwitch.isOn)
			{
				this.COSelf.ship.SetReactorGPMValue("knobRatio", "1");
			}
			else
			{
				this.COSelf.ship.SetReactorGPMValue("knobRatio", "0");
			}
			this.bCycleSafety = this.chkCycleSafety.chkSwitch.isOn;
		}

		private void ToggleTorchSafety()
		{
			float num;
			if (this.chkTorchSafety.chkSwitch.isOn)
			{
				num = this.GetLimiterSafetyMax(this.COSelf.ship);
			}
			else
			{
				num = 1f;
			}
			this._guiOrbitDraw.SetPropMapData("fCrsLimMax", num.ToString());
			this.slidCycle.maxValue = num;
			this.slidFlow.maxValue = Mathf.Min(1f, num * 2f);
			this.bTorchSafety = this.chkTorchSafety.chkSwitch.isOn;
			this._guiOrbitDraw.SetPropMapData("bTorchSafety", this.bTorchSafety.ToString().ToLower());
		}

		[SerializeField]
		private Slider slidFlow;

		[SerializeField]
		private GUIPointerListener gplFlow;

		[SerializeField]
		private GUILamp goTorchSafety;

		[SerializeField]
		private GUILamp goCycleSafety;

		[SerializeField]
		private GUILamp goNWZProx;

		[SerializeField]
		private GUISafetyToggle chkTorchSafety;

		[SerializeField]
		private GUISafetyToggle chkCycleSafety;

		[SerializeField]
		private Slider slidCycle;

		[SerializeField]
		private GUIPointerListener gplCycle;

		[SerializeField]
		private Button btnReactorShutdown;

		[SerializeField]
		private GUILedMeter glmCoreTemp;

		[SerializeField]
		private TMP_Text txtReactantLeft;

		[SerializeField]
		private TMP_Text txtAccel;

		public bool bPauseListenersReactor;

		private bool bTorchSafety = true;

		private bool bCycleSafety = true;
	}
}
