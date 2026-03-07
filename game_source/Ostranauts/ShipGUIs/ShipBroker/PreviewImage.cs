using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class PreviewImage : MonoBehaviour
	{
		public void SetData(Texture mainTex, Texture roomIcon, bool isSilhouette = false)
		{
			if (mainTex == null)
			{
				this.imgMain.gameObject.SetActive(false);
				return;
			}
			this.imgMain.texture = mainTex;
			if (roomIcon == null || roomIcon.name == "missing.png")
			{
				this.imgRoomIcon.gameObject.SetActive(false);
			}
			else
			{
				this.imgRoomIcon.texture = roomIcon;
				this.imgRoomIcon.gameObject.SetActive(true);
			}
			if (this.goBackground != null)
			{
				this.goBackground.gameObject.SetActive(isSilhouette);
			}
		}

		public void SetData(PreviewImage previewImage)
		{
			this.SetData(previewImage.imgMain.texture, previewImage.imgRoomIcon.texture, false);
		}

		[SerializeField]
		public RawImage imgMain;

		[SerializeField]
		public RawImage imgRoomIcon;

		[SerializeField]
		public RawImage goBackground;
	}
}
