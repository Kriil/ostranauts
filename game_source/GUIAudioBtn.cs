using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GUIAudioBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler, IEventSystemHandler
{
	public void OnPointerDown(PointerEventData eventData)
	{
		this.bOn = true;
		if (this.strAudioEmitterDown != null)
		{
			AudioManager.am.PlayAudioEmitter(this.strAudioEmitterDown, false, false);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!this.bOn)
		{
			return;
		}
		this.bOn = false;
		if (this.strAudioEmitterUp != null)
		{
			AudioManager.am.PlayAudioEmitter(this.strAudioEmitterUp, false, false);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!this.bOn)
		{
			return;
		}
		this.bOn = false;
		if (this.strAudioEmitterUp != null)
		{
			AudioManager.am.PlayAudioEmitter(this.strAudioEmitterUp, false, false);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (this.bOn || !Input.GetMouseButton(0))
		{
			return;
		}
		this.bOn = false;
		if (this.strAudioEmitterUp != null)
		{
			AudioManager.am.PlayAudioEmitter(this.strAudioEmitterUp, false, false);
		}
	}

	public string strAudioEmitterDown = "ShipUIBtnNSMinusIn";

	public string strAudioEmitterUp = "ShipUIBtnNSMinusOut";

	private bool bOn;
}
