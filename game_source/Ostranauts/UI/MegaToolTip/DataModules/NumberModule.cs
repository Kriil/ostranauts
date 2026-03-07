using System;
using System.Collections.Generic;
using Ostranauts.UI.MegaToolTip.DataModules.SubElements;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class NumberModule : ModuleBase
	{
		private new void Awake()
		{
			base.Awake();
		}

		public override void SetData(CondOwner co)
		{
			if (co == null || co.mapConds == null)
			{
				this._IsMarkedForDestroy = true;
				return;
			}
			this._numbList.Clear();
			this._co = co;
			int num = 0;
			foreach (Condition condition in co.mapConds.Values)
			{
				if (condition.nDisplayType == 1)
				{
					NumbElement component = UnityEngine.Object.Instantiate<GameObject>(this._numberElement, this._tfNumbContainer.transform).GetComponent<NumbElement>();
					string strData = (condition.fCount * (double)condition.fConversionFactor).ToString("N3") + condition.strDisplayBonus;
					if (condition.strName == "StatGasTemp")
					{
						strData = MathUtils.GetTemperatureString(condition.fCount * (double)condition.fConversionFactor);
					}
					component.SetData(condition.strNameFriendly, condition.strName, strData, GrammarUtils.GetInflectedString(condition.strDesc, condition, co), DataHandler.GetColor(condition.strColor));
					this._numbList.Add(component);
					num++;
					LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
					component.ForceMeshUpdate();
				}
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent as RectTransform);
			if (num == 0)
			{
				this._IsMarkedForDestroy = true;
				return;
			}
		}

		protected override void OnUpdateUI()
		{
			if (this._numbList.Count == 0)
			{
				return;
			}
			foreach (NumbElement numbElement in this._numbList)
			{
				Condition cond = DataHandler.GetCond(numbElement.CondName);
				string strData = (this._co.GetCondAmount(numbElement.CondName) * (double)cond.fConversionFactor).ToString("N3") + cond.strDisplayBonus;
				if (cond.strName == "StatGasTemp")
				{
					strData = MathUtils.GetTemperatureString(this._co.GetCondAmount(numbElement.CondName) * (double)cond.fConversionFactor);
				}
				numbElement.SetData(cond.strNameFriendly, numbElement.CondName, strData, GrammarUtils.GetInflectedString(cond.strDesc, cond, this._co), DataHandler.GetColor(cond.strColor));
			}
			LayoutRebuilder.MarkLayoutForRebuild(base.transform.parent as RectTransform);
		}

		[SerializeField]
		private Transform _tfNumbContainer;

		[SerializeField]
		private GameObject _numberElement;

		private List<NumbElement> _numbList = new List<NumbElement>();

		private CondOwner _co;
	}
}
