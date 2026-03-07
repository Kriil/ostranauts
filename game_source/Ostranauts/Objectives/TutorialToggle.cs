using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	public class TutorialToggle : MonoBehaviour
	{
		private void Awake()
		{
		}

		private void Start()
		{
			this._toggle.isOn = !ObjectiveTracker.ShowTutorials;
			this._toggle.onValueChanged.AddListener(delegate(bool value)
			{
				ObjectiveTracker.OnShowTutorialToggled.Invoke(!value);
			});
			TMP_Text componentInChildren = this._toggle.GetComponentInChildren<TMP_Text>();
			if (componentInChildren != null)
			{
				componentInChildren.text = DataHandler.GetString("GUI_PDA_OBJECTIVE_MUTE_TUTS", false);
			}
		}

		[SerializeField]
		private Toggle _toggle;
	}
}
