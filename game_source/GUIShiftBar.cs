using System;
using UnityEngine;
using UnityEngine.UI;

public class GUIShiftBar : MonoBehaviour
{
	private void Awake()
	{
		this.grad = new Gradient();
		GradientColorKey[] array = new GradientColorKey[3];
		ColorUtility.TryParseHtmlString("#30A7FF", out array[0].color);
		array[0].time = 0f;
		ColorUtility.TryParseHtmlString("#FF9D00", out array[1].color);
		array[1].time = 0.75f;
		ColorUtility.TryParseHtmlString("#FF1200", out array[2].color);
		array[2].time = 1f;
		GradientAlphaKey[] array2 = new GradientAlphaKey[2];
		array2[0].alpha = 1f;
		array2[0].time = 0f;
		array2[1].alpha = 1f;
		array2[1].time = 0f;
		this.grad.SetKeys(array, array2);
	}

	private void Update()
	{
		double num = StarSystem.fEpoch % 31556926.0;
		num %= 2629743.8333333335;
		num %= 87658.12777777777;
		int num2 = Mathf.FloorToInt(Convert.ToSingle(num / 3600.0));
		int num3 = Mathf.FloorToInt((float)(num2 / 6));
		if (num3 >= 4)
		{
			num3 = 3;
		}
		float num4 = (float)num - (float)(num3 * 3600 * 6);
		num4 = Mathf.Clamp(num4 / 3600f / 6f, 0f, 1f);
		num /= 87658.12777777777;
		this.bmpBar.rectTransform.localScale = new Vector3((float)num, 1f, 1f);
		this.bmpBar.color = this.grad.Evaluate(num4);
	}

	[SerializeField]
	private Image bmpBar;

	private Gradient grad;
}
