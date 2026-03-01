using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIChargenCareerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		this.outline.enabled = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.outline.enabled = false;
	}

	private void Start()
	{
		this.outline = base.GetComponentInChildren<Outline>();
		this.outline.effectDistance = new Vector2(3f, 3f);
	}

	public Outline outline;
}
