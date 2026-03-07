using System;
using Ostranauts.UI.MegaToolTip.DataModules.SubElements;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class CondsModule : ModuleBase
	{
		private new void Awake()
		{
		}

		public override void SetData(CondOwner co)
		{
			if (co == null || co.mapConds == null)
			{
				this._IsMarkedForDestroy = true;
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this._condRow, this._tfCondsContainer);
			float num = 0f;
			foreach (Condition condition in co.mapConds.Values)
			{
				if (!string.IsNullOrEmpty(condition.strNameFriendly) && condition.nDisplayOther == 2)
				{
					CondElement component = UnityEngine.Object.Instantiate<GameObject>(this._condElement, gameObject.transform).GetComponent<CondElement>();
					component.SetData(co, condition);
					LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
					component.ForceMeshUpdate();
					num += component.Width + 12f;
					if (num >= this.MaxWidth)
					{
						gameObject = UnityEngine.Object.Instantiate<GameObject>(this._condRow, this._tfCondsContainer);
						component.transform.SetParent(gameObject.transform, false);
						num = component.Width + 10f;
					}
				}
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
		}

		[SerializeField]
		private Transform _tfCondsContainer;

		[SerializeField]
		private GameObject _condElement;

		[SerializeField]
		private GameObject _condRow;

		[SerializeField]
		private float MaxWidth = 250f;
	}
}
