using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class ToggleMoreModule : ModuleBase
	{
		private void Start()
		{
			this._showMore = DataHandler.GetString("GUI_MTT_SHOW_MORE", false);
			this._showLess = DataHandler.GetString("GUI_MTT_SHOW_LESS", false);
			this._button.onClick.AddListener(new UnityAction(this.OnButtonDown));
			this.UpdateText();
		}

		private void OnButtonDown()
		{
			ModuleHost.ToggleShowMore.Invoke();
			this.UpdateText();
		}

		private void UpdateText()
		{
			if (ModuleHost.ShowExpandedTooltip)
			{
				this._tfImg.localScale = new Vector3(1f, -1f, 1f);
				this._txt.text = this._showLess;
			}
			else
			{
				this._tfImg.localScale = new Vector3(1f, 1f, 1f);
				this._txt.text = this._showMore;
			}
		}

		[SerializeField]
		private Button _button;

		[SerializeField]
		private TMP_Text _txt;

		[SerializeField]
		private RectTransform _tfImg;

		private string _showMore = "Show more";

		private string _showLess = "Show less";
	}
}
