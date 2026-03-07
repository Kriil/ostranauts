using System;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.Components
{
	[RequireComponent(typeof(Button))]
	public class ToggleSideSwitch : MonoBehaviour
	{
		public bool State
		{
			get
			{
				return this._state;
			}
			set
			{
				if (value != this._state)
				{
					this.OnButtonClick();
				}
			}
		}

		private void Awake()
		{
			this._button = base.GetComponent<Button>();
			this._button.onClick.AddListener(new UnityAction(this.OnButtonClick));
			this.SetImages(this._state);
		}

		private void OnDestroy()
		{
			this.OnClick.RemoveAllListeners();
		}

		private void OnButtonClick()
		{
			this._state = !this._state;
			this.SetImages(this._state);
			this.OnClick.Invoke(this._state);
		}

		private void SetImages(bool state)
		{
			this.left.SetActive(!state);
			this.right.SetActive(state);
		}

		public void Reset()
		{
			this._state = false;
			this.SetImages(this._state);
		}

		[NonSerialized]
		public OnToggleClicked OnClick = new OnToggleClicked();

		[SerializeField]
		private GameObject left;

		[SerializeField]
		private GameObject right;

		private Button _button;

		private bool _state;
	}
}
