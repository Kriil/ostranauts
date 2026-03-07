using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIScrollHomebrew : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	public void OnPointerExit(PointerEventData eventData)
	{
		this.Entered = false;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		this.Entered = true;
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (this.Entered && Input.GetAxis("Mouse ScrollWheel") != 0f)
		{
			base.transform.GetChild(0).transform.position += new Vector3(0f, -10f) * Input.GetAxis("Mouse ScrollWheel");
		}
	}

	public bool Entered;
}
