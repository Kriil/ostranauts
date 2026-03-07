using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GUIToggleSwap : MonoBehaviour
{
	private void Awake()
	{
		this.targetToggle.toggleTransition = Toggle.ToggleTransition.None;
		this.targetToggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnTargetToggleValueChanged));
	}

	public void OnTargetToggleValueChanged(bool newValue)
	{
		Image image = this.targetToggle.targetGraphic as Image;
		if (image != null)
		{
			if (newValue)
			{
				image.overrideSprite = this.selectedSprite;
			}
			else
			{
				image.overrideSprite = null;
			}
		}
	}

	public Toggle targetToggle;

	public Sprite selectedSprite;
}
