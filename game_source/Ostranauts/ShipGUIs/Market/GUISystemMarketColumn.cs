using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUISystemMarketColumn : MonoBehaviour
	{
		public void SetData(string stationName, int count)
		{
			this.txtStationName.text = stationName;
			if (count % 2 == 0)
			{
				this.imgBackground.color = new Color(1f, 1f, 1f, 0.5f);
			}
		}

		[SerializeField]
		private TMP_Text txtStationName;

		[SerializeField]
		private Image imgBackground;
	}
}
