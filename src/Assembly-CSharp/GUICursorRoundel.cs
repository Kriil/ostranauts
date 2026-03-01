using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(RectTransform))]
public class GUICursorRoundel : MonoBehaviour
{
	private void Start()
	{
		this.initialized = true;
	}

	private void Update()
	{
		if (!this.initialized)
		{
			return;
		}
		this.rectTransform.anchoredPosition = Input.mousePosition;
		if (Input.GetMouseButtonUp(1))
		{
			this.ResetFill();
		}
	}

	public bool FillUp(float time)
	{
		if (!this.initialized)
		{
			return false;
		}
		if (this.fillSecondsCurrent >= this.fillSecondsMax)
		{
			return false;
		}
		this.fillSecondsCurrent += time;
		this.roundelImage.fillAmount = this.FillPercentage();
		return this.fillSecondsCurrent >= this.fillSecondsMax;
	}

	public void ResetFill()
	{
		this.fillSecondsCurrent = 0f;
		this.roundelImage.fillAmount = 0f;
	}

	public float FillPercentage()
	{
		return Mathf.Clamp01(this.fillSecondsCurrent / this.fillSecondsMax);
	}

	[SerializeField]
	public float fillSecondsMax = 1.5f;

	[SerializeField]
	private float fillSecondsCurrent;

	[SerializeField]
	private RectTransform rectTransform;

	[SerializeField]
	private Image roundelImage;

	[SerializeField]
	private bool initialized;
}
