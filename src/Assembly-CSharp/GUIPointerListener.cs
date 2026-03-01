using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIPointerListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
{
	public void OnPointerDown(PointerEventData eventData)
	{
		this.bPressed = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		this.bPressed = false;
	}

	public bool bPressed;
}
