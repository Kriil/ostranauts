using System;
using UnityEngine;
using UnityEngine.UI;

public class GUIAudioToggle : MonoBehaviour
{
	private void Start()
	{
		this.toggle = base.GetComponent<Toggle>();
		this.toggle.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnChange();
		});
	}

	public void OnChange()
	{
		if (this.requiresInit)
		{
			this.requiresInit = false;
			return;
		}
		if (this.strAudioEmitterOff != null && this.toggle.isOn)
		{
			AudioManager.am.PlayAudioEmitter(this.strAudioEmitterOff, false, false);
		}
		if (this.strAudioEmitterOn != null && !this.toggle.isOn)
		{
			AudioManager.am.PlayAudioEmitter(this.strAudioEmitterOn, false, false);
		}
	}

	public string strAudioEmitterOn = "ShipUIBtnNSInnerIn";

	public string strAudioEmitterOff = "ShipUIBtnNSInnerOut";

	private Toggle toggle;

	public bool requiresInit;
}
