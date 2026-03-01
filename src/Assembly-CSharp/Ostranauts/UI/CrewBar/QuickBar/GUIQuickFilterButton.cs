using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.CrewBar.QuickBar
{
	public class GUIQuickFilterButton : MonoBehaviour
	{
		public string Filter
		{
			get
			{
				return this._filter;
			}
		}

		public void SetData(Dictionary<string, Sprite> dictSprites, Dictionary<string, Color> dictColors, Action<string> callback)
		{
			Sprite sprite;
			if (dictSprites.TryGetValue(this._filter, out sprite))
			{
				this._icon.sprite = sprite;
			}
			Color color;
			if (dictColors.TryGetValue(this._filter, out color))
			{
				this._backGroundImage.color = dictColors[this._filter];
			}
			this._button.onClick.AddListener(delegate()
			{
				callback(this._filter);
			});
		}

		public void IncreaseCount()
		{
			this._filterCount++;
		}

		public void ResetCount()
		{
			this._filterCount = 0;
		}

		public bool UpdateElement()
		{
			if (this._filterCount == 0)
			{
				base.gameObject.SetActive(false);
				return false;
			}
			base.gameObject.SetActive(true);
			return true;
		}

		public void ForceOn()
		{
			base.gameObject.SetActive(true);
		}

		[SerializeField]
		private Image _icon;

		[SerializeField]
		private Image _backGroundImage;

		[SerializeField]
		private Button _button;

		[SerializeField]
		private string _filter;

		private int _filterCount;
	}
}
