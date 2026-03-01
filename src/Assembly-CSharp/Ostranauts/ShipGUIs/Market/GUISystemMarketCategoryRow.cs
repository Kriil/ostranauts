using System;
using TMPro;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUISystemMarketCategoryRow : MonoBehaviour
	{
		public void SetData(string collName)
		{
			if (collName != null)
			{
				this.txtCoCollName.text = collName;
			}
		}

		[SerializeField]
		private TMP_Text txtCoCollName;
	}
}
