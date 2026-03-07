using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ostranauts.UI.MegaToolTip
{
	public class TooltipClickHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public void OnPointerClick(PointerEventData eventData)
		{
			if (this.ttp == null && this.ttp == null)
			{
				return;
			}
			if (this.ttp.CO == null)
			{
				return;
			}
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				if (GUIMegaToolTip.Selected == this.ttp.CO && ModuleHost.Opened)
				{
					TooltipPreviewButton.OnPreviewButtonClicked.Invoke(null);
				}
				else
				{
					TooltipPreviewButton.OnPreviewButtonClicked.Invoke(this.ttp.CO);
				}
				GUIMegaToolTip.Selected = this.ttp.CO;
			}
		}

		public TooltipPreviewButton ttp;
	}
}
