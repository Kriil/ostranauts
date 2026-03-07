using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	public class InfoToggle : MonoBehaviour
	{
		private void Awake()
		{
			if (this._toggle == null)
			{
				this._toggle = base.transform.GetComponentInChildren<Toggle>();
			}
		}

		private void Start()
		{
			this._toggle.isOn = ObjectiveTracker.MuteInfoModalTutorials;
			this._toggle.onValueChanged.AddListener(delegate(bool mute)
			{
				ObjectiveTracker.OnMuteInfoToggled.Invoke(mute);
			});
			TMP_Text componentInChildren = this._toggle.GetComponentInChildren<TMP_Text>();
			if (componentInChildren != null)
			{
				componentInChildren.text = DataHandler.GetString("GUI_PDA_OBJECTIVE_MUTE_INFO", false);
			}
		}

		[SerializeField]
		private Toggle _toggle;
	}
}
