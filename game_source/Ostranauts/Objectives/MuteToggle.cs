using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	public class MuteToggle : MonoBehaviour
	{
		private void Awake()
		{
		}

		private void Start()
		{
			this._toggle.isOn = ObjectiveTracker.MuteObjectives;
			this._toggle.onValueChanged.AddListener(delegate(bool mute)
			{
				ObjectiveTracker.OnMuteToggled.Invoke(mute);
			});
			TMP_Text componentInChildren = this._toggle.GetComponentInChildren<TMP_Text>();
			if (componentInChildren != null)
			{
				componentInChildren.text = DataHandler.GetString("GUI_PDA_OBJECTIVE_MUTE_OBJS", false);
			}
		}

		[SerializeField]
		private Toggle _toggle;
	}
}
