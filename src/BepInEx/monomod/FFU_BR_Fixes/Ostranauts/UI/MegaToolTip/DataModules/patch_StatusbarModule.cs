using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class patch_StatusbarModule : StatusbarModule
	{
		public override void OnUpdateUI()
		{
			bool flag = this._co == null;
			if (!flag)
			{
				base.UpdateDamageBar();
				bool flag2 = this._co.HasCond("IsPowerObservable");
				if (flag2)
				{
					double num = 0.0;
					double num2 = 0.0;
					double num3 = 0.0;
					num2 += this._co.GetCondAmount("StatPowerMax") * this._co.GetDamageState();
					num += this._co.GetCondAmount("StatPower");
					List<CondOwner> list = (this._co.Pwr != null && this._co.Pwr.ctPowerSource != null) ? this._co.GetCOs(true, this._co.Pwr.ctPowerSource) : ((this._co.objContainer != null) ? this._co.objContainer.GetCOs(true, null) : new List<CondOwner>());
					bool flag3 = list != null && list.Count > 0;
					if (flag3)
					{
						foreach (CondOwner condOwner in list)
						{
							bool flag4 = condOwner != null;
							if (flag4)
							{
								num2 += condOwner.GetCondAmount("StatPowerMax") * condOwner.GetDamageState();
								num += condOwner.GetCondAmount("StatPower");
							}
						}
					}
					bool flag5 = num2 != 0.0;
					if (flag5)
					{
						num3 = num / num2;
					}
					bool flag6 = num3 > 1.0;
					if (flag6)
					{
						num3 = 1.0;
					}
					string text = "-";
					bool flag7 = this._co.mapInfo.TryGetValue("PowerRemainingTime", out text);
					if (flag7)
					{
						this._txtPower.text = text;
					}
					else
					{
						this._txtPower.text = string.Empty;
					}
					string empty = string.Empty;
					bool flag8 = this._co.mapInfo.TryGetValue("PowerCurrentLoad", out empty);
					if (flag8)
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
				else
				{
					bool flag9 = this._poweredCO != null;
					if (flag9)
					{
						this._sliderStatPower.value = Mathf.Clamp01((float)this._poweredCO.PowerStoredPercent);
					}
				}
			}
		}
	}
}
