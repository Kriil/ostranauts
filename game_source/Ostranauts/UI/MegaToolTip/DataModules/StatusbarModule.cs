using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class StatusbarModule : ModuleBase
	{
		private new void Awake()
		{
			base.Awake();
			this._pnlDamageOverlay.anchorMax = new Vector2(this._dmgThreshold, 1f);
			this._txtDamaged.anchorMax = new Vector2(this._dmgThreshold, 0f);
			this._sliderStatDamage.value = 0f;
			this._tfPowerStatGroup.SetActive(false);
		}

		public override void SetData(CondOwner co)
		{
			this._co = co;
			if (co == null)
			{
				return;
			}
			this._isDamaged = co.HasCond("IsDamaged");
			bool flag = co.HasCond("IsInstalled");
			bool flag2 = flag && co.HasCond("IsPowerStorage");
			bool flag3 = !flag && co.HasCond("IsPowerObservable");
			this._tfPowerStatGroup.SetActive(flag2 || flag3);
			this.OnUpdateUI();
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
		}

		protected override void OnUpdateUI()
		{
			if (this._co == null)
			{
				return;
			}
			this.UpdateDamageBar();
			if (this._co.HasCond("IsPowerObservable"))
			{
				double num = 0.0;
				double num2 = 0.0;
				double num3 = 0.0;
				num2 += this._co.GetCondAmount("StatPowerMax") * this._co.GetDamageState();
				num += this._co.GetCondAmount("StatPower");
				if (this._co.GetComponent<Container>() != null)
				{
					List<CondOwner> cos = this._co.GetComponent<Container>().GetCOs(true, null);
					if (cos != null && cos.Count > 0)
					{
						foreach (CondOwner condOwner in cos)
						{
							if (condOwner != null)
							{
								num2 += condOwner.GetCondAmount("StatPowerMax") * condOwner.GetDamageState();
								num += condOwner.GetCondAmount("StatPower");
							}
						}
					}
				}
				else
				{
					if (StatusbarModule._ctPowerStorage == null)
					{
						StatusbarModule._ctPowerStorage = DataHandler.GetCondTrigger("TIsPowerStorage");
					}
					foreach (CondOwner condOwner2 in this._co.GetCOsSafe(true, StatusbarModule._ctPowerStorage))
					{
						if (!(condOwner2 == null))
						{
							num2 += condOwner2.GetCondAmount("StatPowerMax") * condOwner2.GetDamageState();
							num += condOwner2.GetCondAmount("StatPower");
						}
					}
				}
				if (num2 != 0.0)
				{
					num3 = num / num2;
				}
				if (num3 > 1.0)
				{
					num3 = 1.0;
				}
				string text = "-";
				if (this._co.mapInfo.TryGetValue("PowerRemainingTime", out text))
				{
					this._txtPower.text = text;
				}
				else
				{
					this._txtPower.text = string.Empty;
				}
				string empty = string.Empty;
				if (this._co.mapInfo.TryGetValue("PowerCurrentLoad", out empty))
				{
					this._txtPowerRate.text = empty;
				}
				else
				{
					this._txtPowerRate.text = string.Empty;
				}
				this._sliderStatPower.value = Mathf.Clamp01((float)num3);
				bool active = !string.IsNullOrEmpty(empty) && empty[0] == '+';
				this._arrowContainer.gameObject.SetActive(active);
				this._arrowContainer.anchoredPosition = ((this._sliderStatPower.value >= 0.3f) ? new Vector2(0f, this._arrowContainer.anchoredPosition.y) : new Vector2(14f, this._arrowContainer.anchoredPosition.y));
			}
			else if (this._poweredCO != null)
			{
				this._sliderStatPower.value = Mathf.Clamp01((float)this._poweredCO.PowerStoredPercent);
			}
		}

		private void UpdateDamageBar()
		{
			float num;
			if (this._isDamaged)
			{
				this._pnlDamageOverlay.gameObject.SetActive(false);
				this._fillDamageSlider.color = Color.red;
				num = Mathf.Lerp(0f, this._dmgThreshold, (float)this._co.GetDamageState());
			}
			else
			{
				this._pnlDamageOverlay.gameObject.SetActive(true);
				this._fillDamageSlider.color = Color.green;
				num = Mathf.Lerp(this._dmgThreshold, 1f, (float)this._co.GetDamageState());
			}
			if ((double)num > 0.99 && (double)this._lastUpdateValue < (double)this._dmgThreshold + 0.2)
			{
				this._lastUpdateValue = num;
				return;
			}
			this._lastUpdateValue = num;
			this._sliderStatDamage.value = num;
		}

		[Header("Damage")]
		[SerializeField]
		private Slider _sliderStatDamage;

		[SerializeField]
		private RectTransform _pnlDamageOverlay;

		[SerializeField]
		private RectTransform _txtDamaged;

		[SerializeField]
		private Image _fillDamageSlider;

		[Header("Power")]
		[SerializeField]
		private TMP_Text _txtPower;

		[SerializeField]
		private TMP_Text _txtPowerRate;

		[SerializeField]
		private Slider _sliderStatPower;

		[SerializeField]
		private GameObject _tfPowerStatGroup;

		[SerializeField]
		private RectTransform _arrowContainer;

		private Powered _poweredCO;

		private CondOwner _co;

		private bool _isDamaged;

		private float _dmgThreshold = 0.66f;

		private float _lastUpdateValue = 1f;

		private static CondTrigger _ctPowerStorage;
	}
}
