using System;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip
{
	public class TooltipPreviewButton : MonoBehaviour
	{
		private void Awake()
		{
			if (TooltipPreviewButton.OnPreviewButtonClicked == null)
			{
				TooltipPreviewButton.OnPreviewButtonClicked = new OnTooltipPreviewButtonClickedEvent();
			}
			TooltipPreviewButton.OnPreviewButtonClicked.AddListener(new UnityAction<CondOwner>(this.OnSelectionUpdated));
			TooltipClickHandler tooltipClickHandler = this._btnSelect.gameObject.AddComponent<TooltipClickHandler>();
			tooltipClickHandler.ttp = this;
			TooltipPreviewButton._defaultTextColor = this._txtCOName.color;
		}

		private void OnSelectionUpdated(CondOwner co)
		{
			if (co == null)
			{
				return;
			}
			this.SetData(this.ReacquireCO());
			if (co != this.CO)
			{
				this._txtCOName.color = TooltipPreviewButton._defaultTextColor;
				this._btnSelect.image.color = TooltipPreviewButton._unSelectedColor;
				this._txtCOName.fontStyle = FontStyles.Normal;
			}
			else
			{
				this._btnSelect.image.color = TooltipPreviewButton._selectedColor;
				this._txtCOName.color = TooltipPreviewButton._highlightTextColor;
				this._txtCOName.fontStyle = FontStyles.Bold;
			}
		}

		private CondOwner ReacquireCO()
		{
			if (string.IsNullOrEmpty(this._coStrId))
			{
				return null;
			}
			CondOwner result;
			if (DataHandler.mapCOs.TryGetValue(this._coStrId, out result))
			{
				return result;
			}
			return null;
		}

		public void SetData(CondOwner co)
		{
			this._co = co;
			this._coStrId = ((!(co != null)) ? string.Empty : co.strID);
			if (co == null)
			{
				return;
			}
			this._txtCOName.text = co.ShortName;
			Texture2D texture2D = (!(co.Crew != null) || co.IsRobot) ? DataHandler.LoadPNG(co.strPortraitImg + ".png", false, false) : FaceAnim2.GetPNG(co);
			if (texture2D != null)
			{
				this._imgAspectRatioFitter.aspectRatio = (float)texture2D.width / (float)texture2D.height;
				this._imgCO.texture = texture2D;
			}
		}

		public CondOwner CO
		{
			get
			{
				if (this._co == null)
				{
					this._co = this.ReacquireCO();
				}
				return this._co;
			}
		}

		public string LabelName
		{
			get
			{
				return (!(this._co != null)) ? string.Empty : this._co.ShortName;
			}
		}

		public static OnTooltipPreviewButtonClickedEvent OnPreviewButtonClicked;

		private static readonly Color _unSelectedColor = new Color(1f, 0.7647059f, 0f, 0f);

		private static readonly Color _selectedColor = new Color(1f, 0.7647059f, 0f, 1f);

		private static Color _defaultTextColor;

		private static readonly Color _highlightTextColor = new Color(0.99f, 0.86f, 0f, 1f);

		[SerializeField]
		private TMP_Text _txtCOName;

		[SerializeField]
		private Button _btnSelect;

		[SerializeField]
		private RawImage _imgCO;

		[SerializeField]
		private AspectRatioFitter _imgAspectRatioFitter;

		private CondOwner _co;

		private string _coStrId = string.Empty;
	}
}
