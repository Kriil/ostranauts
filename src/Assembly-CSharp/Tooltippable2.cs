using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltippable2 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private void LoadStrings()
	{
		this.strTtTitle = DataHandler.GetString(this.strTtTitle, true);
		this.strTtBody = DataHandler.GetString(this.strTtBody, true);
	}

	public void SetData(string title, string body, bool loadStrings = false)
	{
		this.strTtTitle = title;
		this.strTtBody = body;
		if (!loadStrings)
		{
			this.bLoaded = true;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (eventData.pointerEnter != null && eventData.pointerEnter.CompareTag("NoToolTip"))
		{
			this.OnPointerExit(eventData);
			return;
		}
		if (!this.bLoaded && CrewSim.objInstance != null)
		{
			this.LoadStrings();
			this.bLoaded = true;
		}
		if (string.IsNullOrEmpty(this.strTtTitle) && string.IsNullOrEmpty(this.strTtBody))
		{
			return;
		}
		GUITooltip2.SetToolTip(this.strTtTitle, this.strTtBody, true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
	}

	public string strTtTitle = string.Empty;

	public string strTtBody = string.Empty;

	private bool bLoaded;
}
