using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleSpriteSwap : MonoBehaviour
{
	private void Awake()
	{
		this.chk = base.GetComponent<Toggle>();
		this.chk.toggleTransition = Toggle.ToggleTransition.None;
		this.chk.onValueChanged.AddListener(new UnityAction<bool>(this.OnTargetToggleValueChanged));
	}

	private void OnTargetToggleValueChanged(bool newValue)
	{
		Image image = this.chk.targetGraphic as Image;
		if (image != null)
		{
			if (newValue)
			{
				image.overrideSprite = null;
			}
			else
			{
				image.overrideSprite = this.bmpOff;
			}
		}
	}

	private Toggle chk;

	public Sprite bmpOff;
}
