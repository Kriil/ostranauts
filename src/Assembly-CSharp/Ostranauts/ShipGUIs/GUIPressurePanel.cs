using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs
{
	public class GUIPressurePanel : MonoBehaviour
	{
		public void SetData(CondOwner co)
		{
			this._co = co;
			if (this._co == null)
			{
				this.lblCoName.text = "-";
				this.imgPortrait.gameObject.SetActive(false);
				return;
			}
			this.lblCoName.text = this._co.strNameFriendly;
			Texture2D texture2D = DataHandler.LoadPNG(this._co.strPortraitImg + ".png", false, false);
			if (texture2D != null)
			{
				this.imgAspect.aspectRatio = (float)texture2D.width / (float)texture2D.height;
				this.imgPortrait.texture = texture2D;
				this.imgPortrait.gameObject.SetActive(true);
			}
			else
			{
				this.imgPortrait.gameObject.SetActive(false);
			}
		}

		private void Update()
		{
			if (StarSystem.fEpoch - this._lastUIUpdate < 0.30000001192092896)
			{
				return;
			}
			this._lastUIUpdate = StarSystem.fEpoch;
			this.txtAbsolute.text = ((!(this._co != null)) ? " - kPa" : (this._co.GetCondAmount("StatGasPressure").ToString("N1") + " KPa"));
		}

		[SerializeField]
		private TMP_Text lblCoName;

		[SerializeField]
		private TMP_Text txtAbsolute;

		[SerializeField]
		private RawImage imgPortrait;

		[SerializeField]
		private AspectRatioFitter imgAspect;

		private CondOwner _co;

		private double _lastUIUpdate;
	}
}
