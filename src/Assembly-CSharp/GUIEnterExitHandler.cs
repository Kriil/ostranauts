using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIEnterExitHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (this.fnOnEnter != null)
		{
			this.fnOnEnter();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (this.fnOnExit != null)
		{
			this.fnOnExit();
		}
	}

	public void SetDelegates(Action onEnter, Action onExit)
	{
		this.fnOnEnter = onEnter;
		this.fnOnExit = onExit;
	}

	public Action fnOnEnter;

	public Action fnOnExit;
}
