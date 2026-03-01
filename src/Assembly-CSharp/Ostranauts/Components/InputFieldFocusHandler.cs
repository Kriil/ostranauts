using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Components
{
	[RequireComponent(typeof(TMP_InputField))]
	public class InputFieldFocusHandler : MonoBehaviour
	{
		private void Start()
		{
			this._inputField = base.GetComponent<TMP_InputField>();
			this._inputField.onSelect.AddListener(this.ToggleTypingOn);
			this._inputField.onDeselect.AddListener(this.ToggleTypingOff);
		}

		private void OnDestroy()
		{
			if (this._inputField != null)
			{
				this._inputField.onSelect.RemoveListener(this.ToggleTypingOn);
				this._inputField.onDeselect.RemoveListener(this.ToggleTypingOff);
			}
		}

		private TMP_InputField _inputField;

		private UnityAction<string> ToggleTypingOn = delegate(string s)
		{
			CrewSim.Typing = true;
		};

		private UnityAction<string> ToggleTypingOff = delegate(string s)
		{
			CrewSim.Typing = false;
		};
	}
}
