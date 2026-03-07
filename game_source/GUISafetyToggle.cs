using System;
using UnityEngine;
using UnityEngine.UI;

public class GUISafetyToggle : MonoBehaviour
{
	private void Awake()
	{
		this.Closed = true;
		this.chkSwitch.isOn = false;
		this.btnClosed.onClick.AddListener(delegate()
		{
			this.Closed = false;
		});
		this.btnOpen.onClick.AddListener(delegate()
		{
			this.Closed = true;
		});
	}

	public bool Closed
	{
		get
		{
			return this.cgClosed.alpha > 0f;
		}
		set
		{
			if (value)
			{
				if (!this.bSilent && this.cgClosed.alpha == 0f)
				{
					AudioManager.am.PlayAudioEmitter("ShipUIBtnNSToggleClose", false, false);
				}
				CanvasManager.ShowCanvasGroup(this.cgClosed);
				CanvasManager.HideCanvasGroup(this.cgOpen);
			}
			else
			{
				if (!this.bSilent && this.cgOpen.alpha == 0f)
				{
					AudioManager.am.PlayAudioEmitter("ShipUIBtnNSToggleOpen", false, false);
				}
				CanvasManager.HideCanvasGroup(this.cgClosed);
				CanvasManager.ShowCanvasGroup(this.cgOpen);
			}
		}
	}

	[SerializeField]
	private Button btnOpen;

	[SerializeField]
	private Button btnClosed;

	[SerializeField]
	private CanvasGroup cgOpen;

	[SerializeField]
	private CanvasGroup cgClosed;

	[SerializeField]
	public Toggle chkSwitch;

	public bool bSilent;
}
