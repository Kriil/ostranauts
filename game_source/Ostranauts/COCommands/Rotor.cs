using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.COCommands
{
	public class Rotor : MonoBehaviour, IManUpdater
	{
		public float Momentum
		{
			get
			{
				return (!this._turnOff) ? (this.Direction.z * Mathf.InverseLerp(Rotor._idleSpeed, Rotor._turboSpeed, this._rotorSpeed)) : 0f;
			}
		}

		private void Awake()
		{
			if (Ship.OnManeuver == null)
			{
				Ship.OnManeuver = new ManeuverEvent();
			}
			Ship.OnManeuver.AddListener(new UnityAction<string, bool>(this.OnPlayerManeuver));
		}

		private IEnumerator Start()
		{
			while (this._coUs == null || this._coUs.ship == null)
			{
				yield return null;
			}
			this.UpdateFromGPM();
			List<CondOwner> activeRotors = this._coUs.ship.aActiveHeavyLiftRotors;
			if (activeRotors != null)
			{
				float num = 0f;
				foreach (CondOwner condOwner in activeRotors)
				{
					if (!(condOwner == null))
					{
						Rotor component = condOwner.GetComponent<Rotor>();
						if (!(component == null))
						{
							num += component.Direction.z;
						}
					}
				}
				this.Direction = ((num > 0f) ? Vector3.back : Vector3.forward);
			}
			int maxTries = 10;
			while (this._audioEmitter == null)
			{
				this._audioEmitter = this._coUs.GetComponent<AudioEmitter>();
				if (this._audioEmitter != null)
				{
					yield break;
				}
				maxTries--;
				yield return new WaitForSeconds(1f);
				if (maxTries <= 0)
				{
					yield break;
				}
			}
			yield break;
		}

		private void OnDestroy()
		{
			Ship.OnManeuver.RemoveListener(new UnityAction<string, bool>(this.OnPlayerManeuver));
			if (this._rotorWheel != null)
			{
				this._rotorWheel.Destroy();
			}
		}

		private void UpdateFromGPM()
		{
			Dictionary<string, string> dictionary = null;
			if (this._coUs.mapGUIPropMaps.TryGetValue(this.strCOKey, out dictionary))
			{
				string empty = string.Empty;
				if (dictionary.TryGetValue("bTurbo", out empty) && bool.Parse(empty))
				{
					this._coUs.SetCondAmount("IsTurboOn", 1.0, 0.0);
				}
				if (dictionary.TryGetValue("nKnobBus", out empty))
				{
					this._coUs.ZeroCondAmount("IsOverrideOff");
					this._coUs.ZeroCondAmount("IsOverrideOn");
					this._coUs.ZeroCondAmount("IsAutoMode");
					this._coUs.ZeroCondAmount("IsTurningOff");
					int num = int.Parse(empty);
					if (num != 1)
					{
						if (num != 2)
						{
							this._coUs.AddCondAmount("IsTurningOff", 1.0, 0.0, 0f);
						}
						else
						{
							this._coUs.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
							this._coUs.ZeroCondAmount("IsOverrideOff");
						}
					}
					else
					{
						this._coUs.AddCondAmount("IsAutoMode", 1.0, 0.0, 0f);
					}
				}
			}
		}

		private void OnPlayerManeuver(string regID, bool maneuvering)
		{
			if (this._coUs == null || this._coUs.ship == null || this._coUs.ship.strRegID != regID || this._turnOff)
			{
				return;
			}
			if (maneuvering)
			{
				float fPitch = 1.1f;
				Powered pwr = this._coUs.Pwr;
				if (this._coUs.HasCond(Rotor.TURBOCOND))
				{
					if (pwr != null)
					{
						pwr.UserPowerExt(pwr.jsonPI.fAmount * 10.0 * (double)CrewSim.TimeElapsedScaled());
					}
					this._rotorSpeed = Rotor._turboSpeed;
					this._turboCounter += CrewSim.TimeElapsedUnscaled() / 2f;
					this._pitchCounter += CrewSim.TimeElapsedUnscaled();
					fPitch = 1.15f;
					if (this._turboCounter > 1f)
					{
						this._turboCounter = 1f;
					}
					if ((double)this._pitchCounter > 1.0)
					{
						this._pitchCounter = 1f;
					}
					AudioManager.am.PlayAudioEmitter("ShipLiftRotorTurbo", true, false);
					AudioManager.am.TweakAudioEmitter("ShipLiftRotorTurbo", this._pitchCounter, this._turboCounter);
				}
				else
				{
					if (pwr != null)
					{
						pwr.UserPowerExt(pwr.jsonPI.fAmount * 5.0 * (double)CrewSim.TimeElapsedScaled());
					}
					this._rotorSpeed = Rotor._maxSpeed;
				}
				if (this._audioEmitter != null)
				{
					this._audioEmitter.TweakSteady(2f, fPitch);
				}
			}
			else
			{
				if (this._audioEmitter != null)
				{
					this._audioEmitter.TweakSteadyReset();
					this._audioEmitter.PlaySteady();
				}
				this._rotorSpeed = Rotor._idleSpeed;
			}
		}

		public void SetData(CondOwner coUs, string rotorCoName, string strCOKey)
		{
			if (coUs == null)
			{
				return;
			}
			this._coUs = coUs;
			this.strCOKey = strCOKey;
			if (string.IsNullOrEmpty(rotorCoName))
			{
				return;
			}
			this._rotorWheel = DataHandler.GetCondOwner(rotorCoName);
			this._rotorWheel.transform.position = this._coUs.transform.position;
			this._rotorWheel.transform.SetParent(base.transform, true);
		}

		private void Update()
		{
			if (!CrewSim.Paused)
			{
				this.Animate();
				this.UnwindTurboSound();
			}
			double num = StarSystem.fEpoch - this.dfEpochLastCheck;
			if (num < this.dfUpdateInterval)
			{
				return;
			}
			if (this._coUs == null || this._coUs.ship == null || CrewSim.system == null)
			{
				return;
			}
			this.Run();
			this.CatchUp();
		}

		private void UnwindTurboSound()
		{
			if (this._turboCounter <= 0f || (double)Math.Abs(this._rotorSpeed - Rotor._turboSpeed) <= 0.01)
			{
				return;
			}
			this._turboCounter -= 2f * CrewSim.TimeElapsedUnscaled();
			this._pitchCounter -= CrewSim.TimeElapsedUnscaled() / 2f;
			AudioManager.am.TweakAudioEmitter("ShipLiftRotorTurbo", this._pitchCounter, this._turboCounter);
			if (this._turboCounter <= 0f)
			{
				this._turboCounter = 0f;
				this._pitchCounter = 0.15f;
				AudioManager.am.StopAudioEmitter("ShipLiftRotorTurbo");
			}
		}

		private void Animate()
		{
			if (this._rotorWheel == null)
			{
				return;
			}
			this._rotorWheel.transform.Rotate(this.Direction * this._rotorSpeed * Time.deltaTime * Time.timeScale);
			this.Transition();
		}

		private void Transition()
		{
			if (!this._turnOff)
			{
				return;
			}
			this._rotorSpeed -= 1.5f;
			float fPitch = Mathf.InverseLerp(0.2f, 1f, this._rotorSpeed / Rotor._idleSpeed);
			if (this._audioEmitter != null)
			{
				this._audioEmitter.TweakSteady(1f, fPitch);
			}
			if (this._rotorSpeed <= 0f)
			{
				this._coUs.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
			}
		}

		private void Run()
		{
			if (this._coUs.HasCond("IsOverrideOn"))
			{
				this._coUs.AddCondAmount("IsOverrideOn", -this._coUs.GetCondAmount("IsOverrideOn"), 0.0, 0f);
				this._coUs.AddCondAmount("IsOverrideOff", -this._coUs.GetCondAmount("IsOverrideOff"), 0.0, 0f);
				if (this._turnOff)
				{
					if (this._audioEmitter != null)
					{
						this._audioEmitter.TweakSteadyReset();
					}
					this._turnOff = false;
					this._rotorSpeed = Rotor._idleSpeed;
				}
			}
			if (this._coUs.HasCond("IsTurningOff"))
			{
				this._coUs.AddCondAmount("IsTurningOff", -this._coUs.GetCondAmount("IsTurningOff"), 0.0, 0f);
				this._turnOff = true;
			}
			if (this._coUs.HasCond("IsAutoMode"))
			{
				if (this._coUs.ship.CurrentRotorEfficiency >= Rotor.AUTOMODETHRESHOLD)
				{
					if (this._coUs.HasCond("IsOff"))
					{
						this._coUs.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
					}
				}
				else if (!this._coUs.HasCond("IsOff") && !this._turnOff)
				{
					this._coUs.AddCondAmount("IsTurningOff", 1.0, 0.0, 0f);
				}
			}
		}

		public void UpdateManual()
		{
			this.Update();
		}

		public void CatchUp()
		{
			this.dfEpochLastCheck = StarSystem.fEpoch;
		}

		public static float ThrustStrength(CondOwner rotor)
		{
			if (rotor == null)
			{
				return 0f;
			}
			double num = rotor.GetCondAmount("StatThrustStrength", false);
			if (rotor.HasCond(Rotor.TURBOCOND))
			{
				num = rotor.GetCondAmount("StatThrustStrengthTurbo", false);
			}
			num *= 30.0;
			return (float)num;
		}

		private static readonly float _idleSpeed = 600f;

		private static readonly float _maxSpeed = 1000f;

		private static readonly float _turboSpeed = 1400f;

		public static readonly string TURBOCOND = "IsTurboOn";

		public static readonly float AUTOMODETHRESHOLD = 0.1f;

		private CondOwner _coUs;

		private CondOwner _rotorWheel;

		private AudioEmitter _audioEmitter;

		private double dfEpochLastCheck = -1.0;

		private double dfUpdateInterval = 1.0;

		private string strSignalCond;

		private string strCOKey;

		[SerializeField]
		private float _rotorSpeed = 600f;

		public Vector3 Direction = Vector3.forward;

		private bool _turnOff;

		private float _turboCounter;

		private float _pitchCounter = 0.15f;
	}
}
