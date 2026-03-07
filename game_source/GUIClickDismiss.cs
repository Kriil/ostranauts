using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIClickDismiss : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private void Awake()
	{
	}

	private void Update()
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (this.bAllowRight && eventData.button == PointerEventData.InputButton.Right)
		{
			base.gameObject.SetActive(false);
		}
		if (this.bAllowLeft && eventData.button == PointerEventData.InputButton.Left)
		{
			base.gameObject.SetActive(false);
		}
	}

	public bool bAllowLeft = true;

	public bool bAllowRight = true;
}
