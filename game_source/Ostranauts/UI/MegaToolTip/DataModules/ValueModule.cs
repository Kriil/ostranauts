using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class ValueModule : ModuleBase, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
	{
		private new void Awake()
		{
			base.Awake();
		}

		private void OnDestroy()
		{
			if (this._isShowingTooltip)
			{
				this.OnPointerExit(null);
			}
		}

		public override void SetData(CondOwner co)
		{
			if (co == null)
			{
				this._IsMarkedForDestroy = true;
				return;
			}
			if (!co.HasCond("StatBasePrice"))
			{
				this._IsMarkedForDestroy = true;
				return;
			}
			this._co = co;
			if (CrewSim.GetSelectedCrew() == null || CrewSim.GetSelectedCrew().HasCond("SkillAdmin"))
			{
				this._Text.text = "~" + this._co.GetBasePrice(true).ToString("C0");
				this.isExact = true;
			}
			else
			{
				double basePrice = this._co.GetBasePrice(true);
				if (basePrice > 30000.0)
				{
					this._Text.text = "$$$$$";
				}
				else if (basePrice > 6000.0)
				{
					this._Text.text = "$$$$";
				}
				else if (basePrice > 900.0)
				{
					this._Text.text = "$$$";
				}
				else if (basePrice > 100.0)
				{
					this._Text.text = "$$";
				}
				else if (basePrice > 10.0)
				{
					this._Text.text = "$";
				}
				this.isExact = false;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			this._isShowingTooltip = true;
			if (this.isExact)
			{
				GUITooltip2.SetToolTip("Precise Value", "A skilled spacer's appraisal of the item's worth.", true);
			}
			else
			{
				GUITooltip2.SetToolTip("Rough Value", "An unskilled spacer's rough guess of the item's worth.", true);
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			this._isShowingTooltip = false;
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		}

		[SerializeField]
		private TMP_Text _Text;

		private CondOwner _co;

		private bool isExact;

		private bool _isShowingTooltip;
	}
}
