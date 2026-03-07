using System;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUIStationMarketTop : MonoBehaviour
	{
		private void Awake()
		{
			this.cgSelectPod.alpha = 0f;
			this.cgSelectGood.alpha = 0f;
			this.HighlightSelectPod();
		}

		public void HighlightSelectPod()
		{
			this._highlightPod = true;
		}

		public void HighlightSelectGood()
		{
			this._highlightGood = true;
		}

		public void HideAll()
		{
			this._highlightGood = false;
			this._highlightPod = false;
			this.cgSelectPod.alpha = 0f;
			this.cgSelectGood.alpha = 0f;
		}

		private void Update()
		{
			if (this._highlightPod)
			{
				this.cgSelectPod.alpha = Mathf.PingPong(Time.unscaledTime, 1f);
			}
			if (this._highlightGood)
			{
				this.cgSelectGood.alpha = Mathf.PingPong(Time.unscaledTime, 1f);
			}
		}

		[SerializeField]
		private CanvasGroup cgSelectPod;

		[SerializeField]
		private CanvasGroup cgSelectGood;

		private bool _highlightPod;

		private bool _highlightGood;
	}
}
