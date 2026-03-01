using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModEngineMode : NavModBase
	{
		protected override void Awake()
		{
			base.Awake();
			this._throttleDelegate = delegate(float A_1)
			{
				this.SetThrottle();
			};
			if (this.chkEngineMode != null)
			{
				this.chkEngineMode.onValueChanged.AddListener(delegate(bool A_1)
				{
					this.SetMode();
				});
			}
			if (this.slidThrottle != null)
			{
				this.slidThrottle.onValueChanged.AddListener(this._throttleDelegate);
			}
			if (this.bmpMeter != null)
			{
				this._meterGreen = this.bmpMeter.color;
			}
		}

		protected override void OnNavModMessage(NavModMessageType messageType, object arg)
		{
			if (messageType != NavModMessageType.UpdateUI)
			{
				if (messageType == NavModMessageType.ManeuverThrustSlider)
				{
					NavModEngineMode x = (NavModEngineMode)arg;
					if (!(x == this) && !(x == null))
					{
						this.SetThrottleNoEvents();
					}
				}
			}
			else
			{
				if (this.COSelf == null || this.COSelf.ship == null || this.bmpMeter == null)
				{
					return;
				}
				float num = this.COSelf.ship.CurrentRotorEfficiency;
				num /= 1.5f;
				this.bmpMeter.color = (((double)num <= 0.25) ? Color.yellow : this._meterGreen);
				this.bmpMeter.fillAmount = num;
				this.bmpMeter.transform.localScale = new Vector3(1f, num, 1f);
			}
		}

		private void SetMode()
		{
			int num = 1;
			if (this.chkEngineMode.isOn)
			{
				num = 3;
			}
			this._guiOrbitDraw.SetPropMapData("nKnobEngineMode", num.ToString());
		}

		private void SetThrottle()
		{
			this._guiOrbitDraw.SetPropMapData("slidThrottle", this.slidThrottle.value.ToString());
			if (CrewSim.coPlayer.HasCond("TutorialNavThrustSliderWaiting", false))
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialNavThrustSliderWaiting");
			}
			GUIOrbitDraw.NavModMessageEvent.Invoke(NavModMessageType.ManeuverThrustSlider, this);
		}

		protected override void Init()
		{
			int num = 0;
			string s;
			if (this.dictPropMap.TryGetValue("nKnobEngineMode", out s))
			{
				num = int.Parse(s);
			}
			if (this.chkEngineMode != null)
			{
				this.chkEngineMode.isOn = (num == 3);
			}
			this.SetThrottleNoEvents();
		}

		private float FetchSliderValueFromDict()
		{
			string s;
			return (!this.dictPropMap.TryGetValue("slidThrottle", out s)) ? 0.25f : float.Parse(s);
		}

		private void SetThrottleNoEvents()
		{
			this.slidThrottle.onValueChanged.RemoveListener(this._throttleDelegate);
			this.slidThrottle.value = this.FetchSliderValueFromDict();
			this.slidThrottle.onValueChanged.AddListener(this._throttleDelegate);
		}

		[SerializeField]
		private Toggle chkEngineMode;

		[SerializeField]
		private Image bmpMeter;

		[SerializeField]
		private Slider slidThrottle;

		private UnityAction<float> _throttleDelegate;

		private Color _meterGreen;
	}
}
