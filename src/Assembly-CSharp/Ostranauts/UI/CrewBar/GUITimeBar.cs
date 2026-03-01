using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.CrewBar
{
	public class GUITimeBar : MonoBehaviour
	{
		private void Awake()
		{
			this.btnFF.onClick.AddListener(delegate()
			{
				CrewSim.TimeScaleMult(2f);
			});
			this.btnHF.onClick.AddListener(delegate()
			{
				CrewSim.TimeScaleMult(0.5f);
			});
			this.btnSFF.onClick.AddListener(delegate()
			{
				CrewSim.ToggleSFF();
			});
			this.chkPlay.onValueChanged.AddListener(delegate(bool A_1)
			{
				if (CrewSim.bPauseLock)
				{
					this.UpdateToggleState();
					return;
				}
				if (this.chkPlay.isOn && !CrewSim.Paused)
				{
					CrewSim.ResetTimeScale();
				}
				CrewSim.Paused = !this.chkPlay.isOn;
			});
		}

		private void Start()
		{
			CrewSim.OnTimeScaleUpdated.AddListener(new UnityAction(this.OnUpdatedTimeScale));
		}

		private void OnDestroy()
		{
			CrewSim.OnTimeScaleUpdated.RemoveListener(new UnityAction(this.OnUpdatedTimeScale));
		}

		private void Update()
		{
			this.txtUTC.text = "UTC " + StarSystem.sUTCEpoch;
			GUIPDA.txtPDAUTC.text = MathUtils.GetTimeFromS(StarSystem.fEpoch);
		}

		private void OnUpdatedTimeScale()
		{
			string text;
			if (!this.commonTimescales.TryGetValue((double)Time.timeScale, out text))
			{
				text = "x" + Time.timeScale.ToString("#.##");
				this.commonTimescales.Add((double)Time.timeScale, text);
			}
			this.txtRate.text = text;
			this.SetButtonReststate(this.btnHF, Time.timeScale < 1f);
			this.SetButtonReststate(this.btnFF, Time.timeScale > 1f);
			this.UpdateToggleState();
		}

		private void UpdateToggleState()
		{
			CrewSim.SetToggleWithoutNotify(this.chkPlay, !CrewSim.Paused);
			CrewSim.SetToggleWithoutNotify(this.chkPause, CrewSim.Paused);
		}

		private void SetButtonReststate(Button button, bool lit)
		{
			if (button == null)
			{
				return;
			}
			ColorBlock colors = button.colors;
			byte b = (!lit) ? 128 : 225;
			colors.normalColor = new Color32(b, b, b, byte.MaxValue);
			button.colors = colors;
		}

		[SerializeField]
		private TMP_Text txtUTC;

		[SerializeField]
		private TMP_Text txtRate;

		[SerializeField]
		private Button btnFF;

		[SerializeField]
		private Button btnSFF;

		[SerializeField]
		private Button btnHF;

		[SerializeField]
		private Toggle chkPause;

		[SerializeField]
		private Toggle chkPlay;

		private Dictionary<double, string> commonTimescales = new Dictionary<double, string>();
	}
}
