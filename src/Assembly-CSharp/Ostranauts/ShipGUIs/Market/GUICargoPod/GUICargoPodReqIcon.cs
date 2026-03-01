using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market.GUICargoPod
{
	public class GUICargoPodReqIcon : MonoBehaviour
	{
		public void SetData(string specImg)
		{
			Texture2D texture2D = DataHandler.LoadPNG(specImg + ".png", false, false);
			if (texture2D != null)
			{
				this.img.texture = texture2D;
			}
		}

		[SerializeField]
		private RawImage img;
	}
}
