using System;
using UnityEngine;

public class ObjectiveSlider : MonoBehaviour
{
	public float SkewPercent
	{
		set
		{
			float num = Mathf.Clamp01(value);
			num = num * (this.fMaxSkew - this.fMinSkew) + this.fMinSkew;
			this.regularObjectives.offsetMax = new Vector2(num, 0f);
			this.regularObjectives.offsetMin = new Vector2(num, 0f);
			this.alarmObjectives.offsetMax = new Vector2(num, 0f);
			this.alarmObjectives.offsetMin = new Vector2(num, 0f);
		}
	}

	public RectTransform regularObjectives;

	public RectTransform alarmObjectives;

	private float fMaxSkew = 500f;

	private float fMinSkew;
}
