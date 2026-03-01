using System;
using System.Collections.Generic;
using Ostranauts.COCommands;
using Ostranauts.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs
{
	public class GUIRotor : GUIData
	{
		private new void Awake()
		{
			base.Awake();
			this.chkTurbo.onValueChanged.AddListener(new UnityAction<bool>(this.OnChangeTurbo));
			this.knobBus.Callback = new Action<int>(this.SetMode);
			this._meterGreen = this.bmpMeter.color;
		}

		public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
		{
			base.Init(coSelf, dict, strCOKey);
			this.lblTitle.text = this.dictPropMap["strTitle"];
			this.LoadCOStats();
		}

		private void LoadCOStats()
		{
			if (this.COSelf == null)
			{
				Debug.LogError("GUIRotor Cannot load null CO: " + this.strCoSelfID);
				return;
			}
			string text = null;
			this._forceSingle = true;
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
				if (this.COSelf.HasCond("IsAutoMode"))
				{
					if (this.knobBus.State != 1)
					{
						this.knobBus.State = 1;
					}
				}
				else if (this.COSelf.HasCond("IsOff"))
				{
					if (this.knobBus.State != 0)
					{
						this.knobBus.State = 0;
					}
				}
				else if (this.knobBus.State != 2)
				{
					this.knobBus.State = 2;
				}
			}
			text = null;
			if (this.dictPropMap.TryGetValue("bTurbo", out text))
			{
				if (bool.Parse(text))
				{
					this.chkTurbo.isOn = true;
				}
				else
				{
					this.chkTurbo.isOn = false;
				}
			}
			this.OnChangeTurbo(this.chkTurbo.isOn);
			this._forceSingle = false;
		}

		private void Update()
		{
			if (this.COSelf == null || this.COSelf.ship == null)
			{
				return;
			}
			double num = StarSystem.fEpoch - this.dfEpochLastCheck;
			if (num < 2.0)
			{
				return;
			}
			float num2 = this.COSelf.ship.CurrentRotorEfficiency;
			num2 /= 1.5f;
			this.bmpMeter.color = (((double)num2 <= 0.25) ? Color.yellow : this._meterGreen);
			this.bmpMeter.fillAmount = num2;
			this.bmpMeter.transform.localScale = new Vector3(1f, num2, 1f);
		}

		private void OnChangeTurbo(bool change)
		{
			List<CondOwner> rotorCOs = this.GetRotorCOs(this._forceSingle || this.chkApplyAllSingle.State);
			foreach (CondOwner condOwner in rotorCOs)
			{
				if (!(condOwner == null))
				{
					if (change)
					{
						condOwner.SetCondAmount(Rotor.TURBOCOND, 1.0, 0.0);
					}
					else
					{
						condOwner.ZeroCondAmount(Rotor.TURBOCOND);
					}
					condOwner.ship.LiftRotorsThrustStrength = -1f;
					condOwner.ApplyGPMChanges(new string[]
					{
						"Panel A,bTurbo," + this.chkTurbo.isOn.ToString().ToLower()
					});
				}
			}
		}

		private List<CondOwner> GetRotorCOs(bool single)
		{
			if (single)
			{
				return new List<CondOwner>
				{
					this.COSelf
				};
			}
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsHeavyLiftRotorInstalled");
			return this.COSelf.ship.GetCOs(condTrigger, false, false, false);
		}

		private void SetMode(int nState)
		{
			List<CondOwner> rotorCOs = this.GetRotorCOs(this._forceSingle || this.chkApplyAllSingle.State);
			foreach (CondOwner condOwner in rotorCOs)
			{
				if (!(condOwner == null))
				{
					condOwner.ZeroCondAmount("IsOverrideOn");
					condOwner.ZeroCondAmount("IsAutoMode");
					condOwner.ZeroCondAmount("IsTurningOff");
					if (nState != 1)
					{
						if (nState != 2)
						{
							condOwner.AddCondAmount("IsTurningOff", 1.0, 0.0, 0f);
						}
						else
						{
							condOwner.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
							condOwner.ZeroCondAmount("IsOverrideOff");
						}
					}
					else
					{
						condOwner.AddCondAmount("IsAutoMode", 1.0, 0.0, 0f);
					}
				}
			}
			base.SetPropMapData("nKnobBus", nState.ToString());
		}

		[SerializeField]
		private TMP_Text lblTitle;

		[SerializeField]
		private GUIKnob knobBus;

		[SerializeField]
		private Toggle chkTurbo;

		[SerializeField]
		private Image bmpMeter;

		[SerializeField]
		private ToggleSideSwitch chkApplyAllSingle;

		private Color _meterGreen;

		private double dfEpochLastCheck = -1.0;

		private bool _forceSingle;
	}
}
