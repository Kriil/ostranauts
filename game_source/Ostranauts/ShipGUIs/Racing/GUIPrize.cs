using System;
using TMPro;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Racing
{
	public class GUIPrize : MonoBehaviour
	{
		public void SetData(double amount, string name)
		{
			this.txtPrize.text = ((amount > 1.0) ? (amount.ToString("F1") + " x " + name) : name);
		}

		public void SetData(float amount)
		{
			this.txtPrize.text = ((amount > 0f) ? ("$" + amount.ToString("N0")) : "-");
		}

		[SerializeField]
		private TMP_Text txtPrize;
	}
}
