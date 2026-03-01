using System;
using TMPro;
using UnityEngine;

public class GUIComputerReadout : MonoBehaviour
{
	private void Start()
	{
		this.text = base.GetComponent<TextMeshProUGUI>();
	}

	private void Update()
	{
		if (this.Buffer.Length > 0 && this.intervalTiming <= 0f)
		{
			TextMeshProUGUI textMeshProUGUI = this.text;
			textMeshProUGUI.text += this.Buffer[0];
			this.Buffer.Remove(0, 1);
			this.intervalTiming = 0.01f;
		}
		if (this.intervalTiming > 0f)
		{
			this.intervalTiming -= Time.deltaTime;
		}
	}

	public string Buffer = string.Empty;

	public TextMeshProUGUI text;

	private float intervalTiming = 0.01f;
}
