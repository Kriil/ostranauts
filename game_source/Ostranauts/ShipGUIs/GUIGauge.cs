using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs
{
	public class GUIGauge : MonoBehaviour
	{
		public void SetData(CondOwner co, string condMax, string condAmount, string postfix = "kPa")
		{
			this._co = co;
			this._condObserve = condAmount;
			this._condNameMax = condMax;
			this._postfix = postfix;
			this.lblCoName.text = ((!(this._co == null)) ? this._co.FriendlyName : string.Empty);
			this.lblToolName.text = "Pressure";
			this.imgGreenFill.fillAmount = this._minFill + 0.7916667f * (this._maxFill - this._minFill);
			this.imgRedFill.fillAmount = this._maxFill - 0.7916667f * (this._maxFill - this._minFill);
		}

		private void UpdateUI()
		{
			if (this._co != null)
			{
				this._cachedMax = (float)this._co.GetCondAmount(this._condNameMax, false);
				this._cachedCurrent = (float)this._co.GetCondAmount(this._condObserve, false);
			}
			float percentage = this._cachedCurrent / Mathf.Max(this._cachedMax, 1E-10f) * 100f;
			this.txtPercentage.text = percentage.ToString("N") + "%";
			this.txtAbsolute.text = this._cachedCurrent.ToString("F1") + " " + this._postfix;
			this.UpdateNeedleRotation(percentage);
		}

		private void UpdateNeedleRotation(float percentage)
		{
			float num = this.ConvertNeedleRotationToPercentage(this.tfRectNeedle.rotation.eulerAngles.z);
			float num2 = percentage - num;
			if (Mathf.Abs(num2) < 0.5f)
			{
				return;
			}
			float num3 = this.ConvertPercentageToNeedleRotation(num + num2 * Time.unscaledDeltaTime);
			if (num3 < -this._needleMax)
			{
				num3 = -this._needleMax;
			}
			else if (num3 > this._needleMax)
			{
				num3 = this._needleMax;
			}
			this.tfRectNeedle.rotation = Quaternion.Euler(0f, 0f, num3);
		}

		private float ConvertPercentageToNeedleRotation(float percentage)
		{
			percentage = Mathf.Min(percentage, 120f);
			float num = percentage / 120f * (2f * this._needleMax);
			return this._needleMax - num;
		}

		private float ConvertNeedleRotationToPercentage(float eulerRotation)
		{
			float num;
			if (eulerRotation > 180f)
			{
				num = this._needleMax + (360f - eulerRotation);
			}
			else
			{
				num = this._needleMax - eulerRotation;
			}
			return num / (2f * this._needleMax) * 120f;
		}

		private void Update()
		{
			if (CrewSim.Paused)
			{
				return;
			}
			this.UpdateUI();
		}

		[Header("General")]
		[SerializeField]
		private TMP_Text lblCoName;

		[SerializeField]
		private TMP_Text lblToolName;

		[SerializeField]
		private RectTransform tfRectNeedle;

		[SerializeField]
		private TMP_Text txtPercentage;

		[SerializeField]
		private TMP_Text txtAbsolute;

		[Header("Color Rings")]
		[SerializeField]
		private Image imgGreenFill;

		[SerializeField]
		private Image imgRedFill;

		private const float _maxPercent = 120f;

		private const float _redMinPercent = 95f;

		private const float _greenMaxPercent = 95f;

		private string _postfix = "kPa";

		private float _minFill = 0.11f;

		private float _maxFill = 0.88f;

		private float _needleMax = 124.5f;

		private CondOwner _co;

		private string _condNameMax;

		private string _condObserve;

		private float _cachedMax = 100f;

		private float _cachedCurrent;
	}
}
