using System;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.PDA
{
	public class PDATimer : MonoBehaviour
	{
		private void Awake()
		{
		}

		private void Start()
		{
			ObjectiveTracker.OnObjectiveComplete.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnObjectiveClosed.AddListener(new UnityAction<Objective>(this.OnObjectiveClosed));
		}

		private void OnDestroy()
		{
			ObjectiveTracker.OnObjectiveComplete.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
			ObjectiveTracker.OnObjectiveClosed.RemoveListener(new UnityAction<Objective>(this.OnObjectiveClosed));
		}

		private void Update()
		{
			if (this._finished)
			{
				return;
			}
			if (!this._counting)
			{
				return;
			}
			this._currentTime = this._targetTime - StarSystem.fEpoch;
			if (this._currentTime <= 0.0)
			{
				this._mainFill.fillAmount = 0f;
				this._bonusFill.fillAmount = 0f;
				this._mainTimer.text = "0s";
				this._bonusTimer.text = this.NumToTime(this._currentTime);
				if (!this._ringing)
				{
					CrewSim.ResetTimeScale();
					this._ringing = true;
					this._objective = new AlarmObjective(AlarmType.pda_timer, CrewSim.GetSelectedCrew(), DataHandler.GetString("PDA_TIMER_FINISHED", false), DataHandler.GetString("PDA_TIMER_FINISHED_DESC", false));
					MonoSingleton<ObjectiveTracker>.Instance.AddObjective(this._objective);
					MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.GetSelectedCrew().strID);
					AudioManager.am.PlayAudioEmitter("PDATimerNotif", true, true);
				}
				if (this._currentTime % 1.0 > -0.5)
				{
					this._mainTimer.alpha = 1f;
				}
				else
				{
					this._mainTimer.alpha = 0f;
				}
				if (this._currentTime <= -30.0)
				{
					this.ResetTimer();
				}
			}
			else
			{
				this._mainTimer.text = this._currentTime.ToString("F0") + "s";
				this._bonusTimer.text = this.NumToTime(this._currentTime);
				this._mainFill.fillAmount = Mathf.Max(0f, (float)(this._currentTime / this._maxTime));
				this._bonusFill.fillAmount = Mathf.Max(0f, (float)((this._currentTime - this._maxTime) / this._maxTime));
				this._mainTimer.alpha = 1f;
			}
		}

		public string NumToTime(double time)
		{
			string empty = string.Empty;
			if (time < 0.0)
			{
				time = 0.0;
			}
			string text = Convert.ToInt32(time % 60.0).ToString("00");
			string text2 = (Math.Floor(time / 60.0) % 60.0).ToString("00");
			string text3 = Math.Floor(time / 60.0 / 60.0).ToString("00");
			return string.Concat(new string[]
			{
				text3,
				":",
				text2,
				":",
				text
			});
		}

		public void ToggleTimer()
		{
			if (this._counting)
			{
				this.StopTimer();
			}
			else
			{
				this.StartTimer();
			}
		}

		public void StartTimer()
		{
			this.StopRinging();
			this._counting = true;
			this._finished = false;
			this._targetTime = StarSystem.fEpoch + this._currentTime;
		}

		public void StopTimer()
		{
			this.StopRinging();
			this._counting = false;
		}

		public void ResetTimer()
		{
			if (this._finished && this._currentTime == this._maxTime)
			{
				this._maxTime = 0.0;
			}
			this._counting = false;
			this._finished = true;
			this._currentTime = this._maxTime;
			this._mainTimer.alpha = 1f;
			if (this._objective != null)
			{
				MonoSingleton<ObjectiveTracker>.Instance.UserSquelchedAlarm(this._objective);
			}
			this.StopRinging();
			this.SetDials();
		}

		private void StopRinging()
		{
			this._ringing = false;
			AudioManager.am.StopAudioEmitter("PDATimerNotif");
		}

		public void ModifyTimer(float amount)
		{
			if (this._finished)
			{
				this._maxTime += (double)amount;
				if (this._maxTime < 0.0)
				{
					this._maxTime = 0.0;
				}
				this._currentTime = this._maxTime;
				this._targetTime = StarSystem.fEpoch + this._currentTime;
			}
			else
			{
				this._currentTime += (double)amount;
				if (this._currentTime < 0.0)
				{
					this._currentTime = 0.0;
				}
				this._targetTime = StarSystem.fEpoch + this._currentTime;
			}
			this.SetDials();
			this._mainTimer.alpha = 1f;
		}

		private void SetDials()
		{
			this._mainTimer.text = this._currentTime.ToString("F0") + "s";
			this._bonusTimer.text = this.NumToTime(this._currentTime);
			this._mainFill.fillAmount = Mathf.Max(0f, (float)(this._currentTime / this._maxTime));
			this._bonusFill.fillAmount = Mathf.Max(0f, (float)((this._currentTime - this._maxTime) / this._maxTime));
		}

		public string CreateCustomInfo()
		{
			string str = string.Empty;
			str = str + "Counting:" + this._counting.ToString();
			str = str + "|Finished:" + this._counting.ToString();
			str = str + "|MaxTime:" + this._maxTime.ToString();
			return str + "|CurrentTime:" + this._currentTime.ToString();
		}

		public void ResolveCustomInfo(string customInfo)
		{
			string[] array = customInfo.Split(new char[]
			{
				'|'
			});
			string[] array2 = array;
			int i = 0;
			while (i < array2.Length)
			{
				string text = array2[i];
				string[] array3 = text.Split(new char[]
				{
					':'
				});
				string text2 = array3[0];
				if (text2 == null)
				{
					goto IL_130;
				}
				if (!(text2 == "Counting"))
				{
					if (!(text2 == "Finished"))
					{
						if (!(text2 == "MaxTime"))
						{
							if (!(text2 == "CurrentTime"))
							{
								goto IL_130;
							}
							try
							{
								this._currentTime = double.Parse(array3[1]);
							}
							catch
							{
								this._currentTime = 0.0;
							}
						}
						else
						{
							try
							{
								this._maxTime = double.Parse(array3[1]);
							}
							catch
							{
								this._maxTime = 0.0;
							}
						}
					}
					else
					{
						try
						{
							this._finished = bool.Parse(array3[1]);
						}
						catch
						{
							this._finished = true;
						}
					}
				}
				else
				{
					try
					{
						this._counting = bool.Parse(array3[1]);
					}
					catch
					{
						this._counting = false;
					}
				}
				IL_13F:
				i++;
				continue;
				IL_130:
				Debug.LogWarning("Timer Custom Info type not recognised!");
				goto IL_13F;
			}
			this.SetDials();
		}

		private void OnObjectiveClosed(Objective obj)
		{
			if (obj == null || this._objective == null)
			{
				return;
			}
			AlarmObjective alarmObjective = obj as AlarmObjective;
			if (alarmObjective == null || alarmObjective.AlarmType != AlarmType.pda_timer)
			{
				return;
			}
			this._objective = null;
		}

		[SerializeField]
		private TextMeshProUGUI _mainTimer;

		[SerializeField]
		private TextMeshProUGUI _bonusTimer;

		[SerializeField]
		private Image _mainFill;

		[SerializeField]
		private Image _bonusFill;

		private AlarmObjective _objective;

		private bool _ringing;

		private bool _finished = true;

		private bool _counting;

		private double _maxTime;

		private double _currentTime;

		private double _targetTime;
	}
}
