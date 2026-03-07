using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.GUIInventory
{
	public class GUIInventoryListElement : MonoBehaviour
	{
		private void Awake()
		{
		}

		public void SetData(string elementName, Texture image)
		{
			this._img.texture = image;
			this._txt.text = elementName;
		}

		public void IncreaseCount()
		{
			this._stackCounter++;
			this._txtStack.text = this._stackCounter.ToString();
			this._pnlStackBackground.SetActive(true);
		}

		[SerializeField]
		private RawImage _img;

		[SerializeField]
		private TextMeshProUGUI _txt;

		[SerializeField]
		private TextMeshProUGUI _txtStack;

		[SerializeField]
		private GameObject _pnlStackBackground;

		private int _stackCounter = 1;
	}
}
