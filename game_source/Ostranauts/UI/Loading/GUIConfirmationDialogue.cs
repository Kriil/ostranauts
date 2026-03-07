using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.Loading
{
	public class GUIConfirmationDialogue : MonoBehaviour
	{
		private void Awake()
		{
			this.btnCancel.onClick.AddListener(new UnityAction(this.OnCancel));
			this.btnConfirm.onClick.AddListener(new UnityAction(this.OnConfirm));
		}

		private void OnConfirm()
		{
			if (this._onConfirmCallback != null)
			{
				this._onConfirmCallback();
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private void OnCancel()
		{
			if (this._onCancelCallback != null)
			{
				this._onCancelCallback();
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}

		public void Setup(string text, Action onConfirmCallback)
		{
			if (!string.IsNullOrEmpty(text))
			{
				this.txtDescription.text = text;
			}
			if (onConfirmCallback == null)
			{
				this.btnCancel.gameObject.SetActive(false);
			}
			this._onConfirmCallback = onConfirmCallback;
		}

		public void Setup(string text, Action onConfirmCallback, Color clrBg, Color clrFg, Color clrFont)
		{
			this.Setup(text, onConfirmCallback);
			this.imgBackground.color = clrBg;
			this.imgForeground.color = clrFg;
			this.txtDescription.color = clrFont;
			this.btnConfirm.GetComponentInChildren<TMP_Text>().color = clrFont;
			this.btnCancel.GetComponentInChildren<TMP_Text>().color = clrFont;
		}

		public void Setup(string text, Action onConfirmCallback, Action onCancelCallback, Color clrBg, Color clrFg, Color clrFont)
		{
			this.Setup(text, onConfirmCallback);
			this._onCancelCallback = onCancelCallback;
			this.imgBackground.color = clrBg;
			this.imgForeground.color = clrFg;
			this.txtDescription.color = clrFont;
			this.btnConfirm.GetComponentInChildren<TMP_Text>().color = clrFont;
			this.btnCancel.GetComponentInChildren<TMP_Text>().color = clrFont;
		}

		[SerializeField]
		private TMP_Text txtDescription;

		[SerializeField]
		private Button btnConfirm;

		[SerializeField]
		private Button btnCancel;

		[SerializeField]
		private Image imgBackground;

		[SerializeField]
		private Image imgForeground;

		private Action _onConfirmCallback;

		private Action _onCancelCallback;
	}
}
