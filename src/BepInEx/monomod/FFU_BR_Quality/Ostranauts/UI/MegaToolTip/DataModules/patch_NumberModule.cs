using System;
using FFU_Beyond_Reach;
using Ostranauts.UI.MegaToolTip.DataModules.SubElements;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	// Quality patch for numeric tooltip rows.
	// This extends number rendering so gas temperature can show the optional
	// alternate unit beside Kelvin, matching the FFU_BR quality config.
	public class patch_NumberModule : NumberModule
	{
	// Builds the numeric rows for the current tooltip target.
	// The special-case `StatGasTemp` formatting is the FFU_BR addition here.
	public void SetData(CondOwner co)
		{
			bool flag = co == null || co.mapConds == null;
			if (flag)
			{
				this._IsMarkedForDestroy = true;
			}
			else
			{
				this._numbList.Clear();
				this._co = co;
				int num = 0;
				foreach (Condition condition in co.mapConds.Values)
				{
					bool flag2 = condition.nDisplayType == 1;
					if (flag2)
					{
						NumbElement component = Object.Instantiate<GameObject>(this._numberElement, this._tfNumbContainer.transform).GetComponent<NumbElement>();
						bool flag3 = condition.strName == "StatGasTemp";
						string text;
						if (flag3)
						{
							bool altTempEnabled = FFU_BR_Defs.AltTempEnabled;
							if (altTempEnabled)
							{
								double num2 = condition.fCount * (double)condition.fConversionFactor;
								double num3 = condition.fCount * (double)FFU_BR_Defs.AltTempMult + (double)FFU_BR_Defs.AltTempShift;
								text = string.Concat(new string[]
								{
									num2.ToString("N3"),
									condition.strDisplayBonus,
									" | ",
									num3.ToString("N1"),
									FFU_BR_Defs.AltTempSymbol
								});
							}
							else
							{
								text = MathUtils.GetTemperatureString(condition.fCount * (double)condition.fConversionFactor);
							}
						}
						else
						{
							text = (condition.fCount * (double)condition.fConversionFactor).ToString("N3") + condition.strDisplayBonus;
						}
						component.SetData(condition.strNameFriendly, condition.strName, text, GrammarUtils.GetInflectedString(condition.strDesc, condition, co), DataHandler.GetColor(condition.strColor));
						this._numbList.Add(component);
						num++;
						LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
						component.ForceMeshUpdate();
					}
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
				LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent as RectTransform);
				bool flag4 = num == 0;
				if (flag4)
				{
					this._IsMarkedForDestroy = true;
				}
			}
		}
	// Refreshes existing rows after the tooltip target changes in-place.
	// This keeps the alternate temperature display synchronized with live values.
	protected void OnUpdateUI()
		{
			bool flag = this._numbList.Count == 0;
			if (!flag)
			{
				foreach (NumbElement numbElement in this._numbList)
				{
					Condition cond = DataHandler.GetCond(numbElement.CondName);
					double condAmount = this._co.GetCondAmount(numbElement.CondName);
					bool flag2 = cond.strName == "StatGasTemp";
					string text;
					if (flag2)
					{
						bool altTempEnabled = FFU_BR_Defs.AltTempEnabled;
						if (altTempEnabled)
						{
							double num = condAmount * (double)cond.fConversionFactor;
							double num2 = condAmount * (double)FFU_BR_Defs.AltTempMult + (double)FFU_BR_Defs.AltTempShift;
							text = string.Concat(new string[]
							{
								num.ToString("N3"),
								cond.strDisplayBonus,
								" | ",
								num2.ToString("N1"),
								FFU_BR_Defs.AltTempSymbol
							});
						}
						else
						{
							text = MathUtils.GetTemperatureString(condAmount * (double)cond.fConversionFactor);
						}
					}
					else
					{
						text = (condAmount * (double)cond.fConversionFactor).ToString("N3") + cond.strDisplayBonus;
					}
					numbElement.SetData(cond.strNameFriendly, numbElement.CondName, text, GrammarUtils.GetInflectedString(cond.strDesc, cond, this._co), DataHandler.GetColor(cond.strColor));
				}
				LayoutRebuilder.MarkLayoutForRebuild(base.transform.parent as RectTransform);
			}
		}
	}
}
