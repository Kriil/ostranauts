using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIBtnPressHold : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		this.bPressed = Input.GetMouseButton(0);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.bPressed = false;
	}

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
