using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules.SubElements
{
	public class CondElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
	{
		private void Awake()
		{
		}

		private void OnDestroy()
		{
			if (this._isShowingTooltip)
			{
				this.OnPointerExit(null);
			}
		}

		public void SetData(CondOwner co, Condition cond)
		{
			this._co = co;
			this._cond = cond;
			this._txtCondName.text = this.UpdateString();
			this._strTextLast = this._txtCondName.text;
		}

		private string UpdateString()
		{
			if (this._co == null || this._co.mapConds == null)
			{
				return this._cond.strNameFriendly;
			}
			Color color = DataHandler.GetColor(this._cond.strColor);
			float num;
			float num2;
			float num3;
			Color.RGBToHSV(color, out num, out num2, out num3);
			Color color2 = new Color(0.12f, 0.12f, 0.12f);
			if (num3 < 0.5f)
			{
				color2 = new Color(0.88f, 0.88f, 0.88f);
			}
			Condition condition = null;
			if (this._cond.strTrackCond != null && this._cond.strTrackCond != this._cond.strName)
			{
				this._co.mapConds.TryGetValue(this._cond.strTrackCond, out condition);
			}
			if (condition == null)
			{
				condition = this._cond;
			}
			string statusText;
			if (!this._cond.bInvert)
			{
				this._pnlBackground.color = color2;
				this._txtCondName.color = color;
				statusText = GUIStatus.GetStatusText(condition, color);
			}
			else
			{
				this._pnlBackground.color = color;
				this._txtCondName.color = color2;
				statusText = GUIStatus.GetStatusText(condition, color2);
			}
			return this._cond.strNameFriendly + statusText;
		}

		private void Update()
		{
			string text = this.UpdateString();
			if (text == this._strTextLast)
			{
				return;
			}
			this._txtCondName.text = text;
			this._strTextLast = this._txtCondName.text;
			this.ForceMeshUpdate();
		}

		public void ForceMeshUpdate()
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			this._txtCondName.ForceMeshUpdate();
		}

		public float Width
		{
			get
			{
				this._width = this._txtCondName.textBounds.size.x;
				return this._width;
			}
		}

		public float Height
		{
			get
			{
				this._height = this._txtCondName.textBounds.size.y + 2f;
				return this._height;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (this._cond == null || this._cond.strName == string.Empty)
			{
				return;
			}
			this._isShowingTooltip = true;
			GUITooltip2.SetToolTip(this._cond.strNameFriendly, GrammarUtils.GetInflectedString(this._cond.strDesc, this._cond, GUIMegaToolTip.Selected), true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			this._isShowingTooltip = false;
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		}

		[SerializeField]
		private TMP_Text _txtCondName;

		[SerializeField]
		private Image _pnlBackground;

		[SerializeField]
		private float _width;

		[SerializeField]
		private float _height;

		private CondOwner _co;

		private Condition _cond;

		private string _strTextLast;

		private bool _isShowingTooltip;
	}
}
