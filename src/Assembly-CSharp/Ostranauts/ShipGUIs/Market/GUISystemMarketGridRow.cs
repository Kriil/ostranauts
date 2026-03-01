using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Market
{
	public class GUISystemMarketGridRow : MonoBehaviour
	{
		public void SetData(IEnumerable<string> stationNames, Dictionary<string, float> marketData)
		{
			int num = 0;
			foreach (string key in stationNames)
			{
				GUISystemMarketColumn component = UnityEngine.Object.Instantiate<GameObject>(this.prefabGridElement, base.transform).GetComponent<GUISystemMarketColumn>();
				float num2;
				marketData.TryGetValue(key, out num2);
				if (num2 == 0f)
				{
					component.SetData("-", num);
				}
				else
				{
					string text = "<color=#A9A9A9>";
					num2 -= 1f;
					if (num2 != 0f)
					{
						text = ((num2 <= 0f) ? "<color=green>" : "<color=red>");
					}
					string stationName = string.Concat(new object[]
					{
						text,
						(num2 <= 0f) ? string.Empty : "+",
						(int)(num2 * 100f),
						"%</color>"
					});
					component.SetData(stationName, num);
				}
				num++;
			}
		}

		[SerializeField]
		private GameObject prefabGridElement;
	}
}
