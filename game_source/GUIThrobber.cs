using System;
using System.Collections.Generic;
using UnityEngine;

public class GUIThrobber : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (this.m_active)
		{
			this.m_timeLine += Time.deltaTime;
			float num = (Mathf.Sin(this.m_frequency * this.m_timeLine) + 1f) / 2f * this.m_amplitude + this.m_min;
			foreach (RectTransform rectTransform in this.m_vertLines)
			{
				rectTransform.sizeDelta = new Vector2(num, rectTransform.sizeDelta.y);
			}
			foreach (RectTransform rectTransform2 in this.m_horLines)
			{
				rectTransform2.sizeDelta = new Vector2(rectTransform2.sizeDelta.x, num);
			}
		}
	}

	public void StartTurn()
	{
		this.m_active = true;
		this.m_timeLine = 0f;
		foreach (RectTransform rectTransform in this.m_vertLines)
		{
			rectTransform.gameObject.SetActive(true);
		}
		foreach (RectTransform rectTransform2 in this.m_horLines)
		{
			rectTransform2.gameObject.SetActive(true);
		}
	}

	public void EndTurn()
	{
		this.m_active = false;
		foreach (RectTransform rectTransform in this.m_vertLines)
		{
			rectTransform.gameObject.SetActive(false);
		}
		foreach (RectTransform rectTransform2 in this.m_horLines)
		{
			rectTransform2.gameObject.SetActive(false);
		}
	}

	public bool m_active;

	public float m_min = 3f;

	public float m_amplitude = 6f;

	public float m_frequency = 4f;

	public float m_timeLine;

	public List<RectTransform> m_vertLines = new List<RectTransform>();

	public List<RectTransform> m_horLines = new List<RectTransform>();
}
